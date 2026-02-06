# Propentro API - Architecture Deep Analysis

> Bu doküman, projenin feature geliştirmeye başlamadan önceki durumunu kapsamlı şekilde analiz eder. Her bir isteğin izlediği yol, pipeline sırası, ve tespit edilen sorunlar detaylıca açıklanmıştır.

---

## 1. Request Lifecycle (Bir İsteğin İzlediği Tam Yol)

```
Client HTTP Request
        │
        ▼
┌─────────────────────────────────┐
│  1. GlobalExceptionHandler      │  ← app.UseExceptionHandler()
│     Tüm unhandled exceptionları │
│     yakalar, ProblemDetails döner│
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  2. Serilog Request Logging     │  ← app.UseSerilogRequestLogging()
│     Her request'i loglar        │
│     (method, path, status, ms)  │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  3. Authentication Middleware   │  ← app.UseAuthentication()
│     JWT Bearer → Clerk JWKS    │
│     Claims: sub, org_id, email │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  4. Authorization Middleware    │  ← app.UseAuthorization()
│     PermissionAuthorizationHandler çalışır
│     → ICurrentUser kontrol eder │
│     → PermissionService ile     │
│       DB'den yetkileri çeker    │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  5. UserContextMiddleware       │  ← app.UseMiddleware<>()
│     ClerkIdResolver → DB lookup │
│     External ID → Internal ID   │
│     Serilog LogContext'e push    │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  6. Endpoint (Minimal API)      │  ← app.MapApiEndpoints()
│     Request DTO → Command/Query │
│     ISender.Send() ile MediatR  │
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────┐
│  7. MediatR Pipeline Behaviors              │
│                                             │
│  7a. LoggingBehavior                        │
│      → [START] RequestName, UserId, OrgId   │
│      → Stopwatch başlat                     │
│      → [END] RequestName, Duration          │
│                                             │
│  7b. ValidationBehavior                     │
│      → FluentValidation validators çalıştır │
│      → Hata varsa ValidationException fırlat│
│                                             │
│  7c. UnitOfWorkBehavior                     │
│      → Sadece ICommand için çalışır         │
│      → BEGIN TRANSACTION                    │
│      → Handler çalıştır                     │
│      → Başarı → COMMIT                      │
│      → Failure Result → ROLLBACK            │
│      → Exception → ROLLBACK + throw         │
│                                             │
│  7d. QueryCachingBehavior                   │
│      → Sadece ICachedQuery için çalışır     │
│      → Cache hit → direkt dön              │
│      → Cache miss → handler çalıştır, cache │
└─────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  8. Handler                     │
│     Command → EF Core (write)   │
│     Query → Dapper (read)       │
│     Return Result<T> veya Result│
└─────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────┐
│  9. ResultExtensions            │
│     Result → IResult (HTTP)     │
│     Success → 200/201/204       │
│     Failure → 400/404/409/etc   │
└─────────────────────────────────┘
        │
        ▼
Client HTTP Response
```

### Pipeline Sırası Doğrulaması

**Program.cs sırası doğru mu?** Evet, middleware sırası doğru:
1. `UseExceptionHandler()` → En dışta, her şeyi yakalar ✅
2. `UseSerilogRequestLogging()` → Exception handler'dan sonra ✅
3. `UseAuthentication()` → JWT claims'i parse eder ✅
4. `UseAuthorization()` → Auth'dan sonra ✅
5. `UseMiddleware<UserContextMiddleware>()` → Auth'dan sonra ✅
6. `MapApiEndpoints()` → En son ✅

**MediatR Pipeline sırası doğru mu?** Evet:
1. Logging → Her şeyi loglar ✅
2. Validation → İş mantığına girmeden kontrol ✅
3. UnitOfWork → Transaction yönetimi ✅
4. QueryCaching → Handler'dan önce cache kontrol ✅

---

## 2. KRİTİK SORUNLAR (Veri Kaybı / Güvenlik)

### 🔴 SORUN #1: Multi-Tenancy Global Filter — Ciddi Güvenlik Açığı

**Dosya:** `Infrastructure/Persistence/Extensions/ModelBuilderExtensions.cs:28`

```csharp
var organizationId = Expression.Property(
    Expression.Constant(currentUser),  // ← BU SATIR SORUNLU
    nameof(ICurrentUser.OrganizationId));
```

**Sorun:** `Expression.Constant(currentUser)` kullanılarak ICurrentUser instance'ı expression tree'ye sabit olarak gömülüyor. EF Core modeli ilk request'te build edip cache'liyor. Sonraki tüm request'lerde cache'lenmiş model kullanılıyor — bu da ilk request'in `currentUser` instance'ını referans almaya devam ediyor.

**Sonuç:** İlk kullanıcının `OrganizationId`'si TÜM sonraki isteklerde kullanılır. Başka bir organizasyonun kullanıcısı sisteme girdiğinde, ilk kullanıcının verilerini görür. **Bu bir cross-tenant data leak'tir.**

**Nasıl olmalı:** EF Core'un standard multi-tenancy pattern'i kullanılmalı:
```csharp
// AppDbContext'e property ekle
public Guid CurrentOrganizationId => currentUser.OrganizationId;

// OnModelCreating'de lambda closure kullan (Expression.Constant DEĞİL)
// EF Core, DbContext field/property closure'larını her query'de yeniden değerlendirir
```

**Ciddiyet:** 🔴 KRİTİK — Production'da veri sızıntısı oluşturur.

---

### 🔴 SORUN #2: SaveChangesAsync Çağrılmıyor (10 Handler'da)

**CLAUDE.md'de açıkça belirtiliyor:**
> Handler **MUST** call `SaveChangesAsync` to persist changes
> UnitOfWorkBehavior only manages transaction (Begin/Commit/Rollback)

**Etkilenen handler'lar:**

| Handler | Dosya | SaveChangesAsync? |
|---------|-------|:-:|
| CreatePropertyHandler | `Properties/Commands/CreateProperty/CreatePropertyHandler.cs` | ✅ Çağırıyor |
| CreateUnitHandler | `Units/Commands/CreateUnit/CreateUnitHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| CreateLeaseHandler | `Leases/Commands/CreateLease/CreateLeaseHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| CreatePartyHandler | `Parties/Commands/CreateParty/CreatePartyHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| UpdatePropertyHandler | `Properties/Commands/UpdateProperty/UpdatePropertyHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| UpdateUnitHandler | `Units/Commands/UpdateUnit/UpdateUnitHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| UpdateLeaseHandler | `Leases/Commands/UpdateLease/UpdateLeaseHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| UpdatePartyHandler | `Parties/Commands/UpdateParty/UpdatePartyHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| DeletePropertyHandler | `Properties/Commands/DeleteProperty/DeletePropertyHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| DeleteUnitHandler | `Units/Commands/DeleteUnit/DeleteUnitHandler.cs` | ❌ **ÇAĞIRMIYOR** |
| DeleteLeaseHandler | `Leases/Commands/DeleteLease/DeleteLeaseHandler.cs` | ❌ **ÇAĞIRMIYOR** |

**Ne oluyor:**
1. UnitOfWorkBehavior → `BEGIN TRANSACTION`
2. Handler çalışır, entity'yi ChangeTracker'a ekler ama `SaveChangesAsync` ÇAĞIRMAZ
3. UnitOfWorkBehavior → `COMMIT` — Ama hiçbir SQL INSERT/UPDATE çalışmadı!
4. Transaction boş olarak commit olur
5. Veri veritabanına **hiç yazılmaz**

**Ek sorun (Create handler'lar için):** `PublicId` veritabanı tarafından generate ediliyor. SaveChanges olmadan PublicId `default!` (null/empty) döner. API, kullanıcıya boş bir ID döner.

**Ciddiyet:** 🔴 KRİTİK — Hiçbir write işlemi persist olmaz.

---

### 🔴 SORUN #3: ClerkIdResolver — Concurrent Queries on Single Connection

**Dosya:** `Infrastructure/Services/ClerkIdResolver.cs:46`

```csharp
await Task.WhenAll(userTask, orgTask);
```

Hem `ResolveUserAsync` hem `ResolveOrgAsync` aynı `NpgsqlConnection` üzerinde paralel çalışıyor. Npgsql (multiplexing olmadan) tek bir connection üzerinde concurrent command desteklemez.

**Ne zaman oluşur:** Hem user hem org cache'de yokken (ilk erişim) — iki query aynı anda çalışır.

**Hata:** `System.InvalidOperationException: An operation is already in progress`

**Ciddiyet:** 🔴 İlk kez giriş yapan kullanıcılarda 500 hatası.

---

## 3. YÜKSEK ÖNCELİKLİ SORUNLAR (Fonksiyonel Hatalar)

### 🟠 SORUN #4: Lease Create — Unit ve Party Bağlantısı Yok

**Dosya:** `Api/Endpoints/LeaseEndpoints.cs:91-102`

`CreateLeaseRequest` contract'ı `Units` ve `Parties` array'leri içeriyor:
```csharp
// Contracts/Leases/CreateLeaseRequest.cs
IReadOnlyList<CreateLeaseUnitRequest> Units   // ← API'de var
IReadOnlyList<CreateLeasePartyRequest> Parties // ← API'de var
```

Ama `CreateLeaseCommand` bunları **içermiyor**:
```csharp
// Application/Leases/Commands/CreateLease/CreateLeaseCommand.cs
public sealed record CreateLeaseCommand(
    string? LeaseType,
    DateOnly StartDate,
    // ... temel alanlar
    // Units YOK, Parties YOK
) : ICommand<string>;
```

Endpoint'te de mapping yok:
```csharp
var command = new CreateLeaseCommand(
    request.LeaseType,
    request.StartDate,
    // ... request.Units ve request.Parties HİÇ KULLANILMIYOR
);
```

**Sonuç:** Lease yaratılır ama hiçbir unit veya party'ye bağlanmaz. `lease_units` ve `lease_parties` tabloları boş kalır.

---

### 🟠 SORUN #5: Eksik FluentValidation Validator'ları

Sadece **1 validator** var: `CreatePropertyValidator`

| Command | Validator | Durum |
|---------|-----------|-------|
| CreatePropertyCommand | CreatePropertyValidator | ✅ |
| CreateUnitCommand | — | ❌ Yok |
| CreateLeaseCommand | — | ❌ Yok |
| CreatePartyCommand | — | ❌ Yok |
| UpdatePropertyCommand | — | ❌ Yok |
| UpdateUnitCommand | — | ❌ Yok |
| UpdateLeaseCommand | — | ❌ Yok |
| UpdatePartyCommand | — | ❌ Yok |

**Sonuç:**
- Boş `Name` ile Unit oluşturulabilir
- Geçersiz `Kind` ile Party oluşturulabilir
- Geçersiz `Status` ile Lease güncellenebilir
- Negatif `RentMinor` ile Lease oluşturulabilir
- `PropertyKind` validasyonu sadece Property'de var, diğer enum-like string'ler (UnitType, LeaseType, Status, vb.) hiç validate edilmiyor

---

### 🟠 SORUN #6: API Contracts — PropertyId Tipi Guid vs PublicId

**Dosya:** `Contracts/Units/CreateUnitRequest.cs`

```csharp
public record CreateUnitRequest(
    Guid PropertyId,  // ← Internal Guid ID bekleniyor
    // ...
);
```

**Sorun:** API kullanıcıları `PublicId` (string, örn: `"prop_abc123"`) bilir, internal `Guid Id`'yi bilmez. Ama `CreateUnitCommand` ve `CreateUnitHandler` internal `Guid` ile çalışıyor:

```csharp
// CreateUnitHandler.cs
var propertyExists = await db.Properties
    .AnyAsync(p => p.Id == request.PropertyId, cancellationToken);  // Internal Guid
```

Bu, API tasarım hatası. Kullanıcı PropertyId olarak Guid gönderemez çünkü onu bilmez.

**Aynı sorun:** `GetLeasesQuery.UnitId` da `Guid?` — kullanıcı Unit'in internal Guid'ini bilmez.

---

### 🟠 SORUN #7: PermissionService SQL — Olmayan Kolon Referansı

**Dosya:** `Infrastructure/Services/PermissionService.cs:44`

```sql
AND p.is_active = TRUE    -- 'p' = permissions tablosu
```

Ama `Permission` entity'sinde `IsActive` alanı **yok**:
```csharp
public sealed class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string GroupName { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    // IsActive YOK — ISoftDeletable implement etmiyor
}
```

**Sonuç:** Eğer DB'de `is_active` kolonu yoksa, bu SQL runtime'da `NpgsqlException: column p.is_active does not exist` hatası verir. Veritabanında her authorization check'te 500 döner.

---

## 4. ORTA ÖNCELİKLİ SORUNLAR (Production Readiness)

### 🟡 SORUN #8: CORS Konfigürasyonu Yok

**Dosya:** `Api/Program.cs`

`AddCors()` ve `UseCors()` çağrısı yok. Frontend farklı bir domain'den API'ye request atarsa, browser CORS hatası alır.

---

### 🟡 SORUN #9: Health Check Endpoint Yok

`/health` veya `/ready` endpoint'i yok. Kubernetes, load balancer veya monitoring araçları API durumunu kontrol edemez.

---

### 🟡 SORUN #10: Testler Tamamen Boş

4 test projesi var ama **hiçbirinde test dosyası yok**:
- `Propentro.Domain.Tests/` — 0 test
- `Propentro.Application.Tests/` — 0 test
- `Propentro.Api.Tests/` — 0 test
- `Propentro.Integration.Tests/` — 0 test

Sadece `.csproj` dosyaları mevcut. Framework ve bağımlılıklar doğru ayarlanmış (xUnit, FluentAssertions, NSubstitute, Testcontainers) ama hiç test yazılmamış.

---

### 🟡 SORUN #11: Database Migration'lar Yok

`Infrastructure/Migrations/` klasörü yok. Veritabanı şeması oluşturmak için migration dosyaları gerekiyor. Şu anda `dotnet ef database update` çalıştırılamaz.

---

### 🟡 SORUN #12: BlobStorageService Placeholder

**Dosya:** `Infrastructure/Services/BlobStorageService.cs`

Tüm method'lar TODO — Cloudflare R2 SDK entegrasyonu yapılmamış. `UploadAsync`, `DownloadAsync`, vb. gerçek implementasyonu yok. Singleton olarak register ediliyor ama işlevsel değil.

---

### 🟡 SORUN #13: Update Handler'larda Null ile Clear Yapılamıyor

Tüm update handler'lar aynı pattern'i kullanıyor:
```csharp
if (request.Note is not null) { property.Note = request.Note; }
```

Bu pattern, bir alanı `null` yapmayı (temizlemeyi) engelliyor. Kullanıcı bir `Note`'u silmek isterse `null` gönderir, ama handler bunu "değişiklik yok" olarak yorumlar ve skip eder.

**Çözüm seçenekleri:**
- `Optional<T>` wrapper type kullanmak (sent vs. not-sent ayrımı)
- JSON Patch (RFC 6902) kullanmak
- Explicit `ClearNote = true` gibi flag'ler eklemek

---

### 🟡 SORUN #14: API Versioning Package Kullanılmıyor

`Asp.Versioning.Http` NuGet paketi referans edilmiş ama Program.cs'de hiç konfigüre edilmemiş. Endpoint'ler manual olarak `/api/v1/` prefix'i kullanıyor.

---

### 🟡 SORUN #15: Domain Event'lerde DateTime.UtcNow Kullanılıyor

**Dosya:** Tüm event'ler (`Domain/Events/*.cs`)

```csharp
public sealed record PropertyCreatedEvent(...) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;  // ← Direkt kullanım
}
```

`IDateTimeProvider` inject edilmemiş. Test'lerde zaman kontrol edilemez.

---

## 5. DÜŞÜK ÖNCELİKLİ SORUNLAR (Kod Kalitesi)

### SORUN #16: Rate Limiting Yok
API'ye sınırsız request atılabilir. `builder.Services.AddRateLimiter()` kullanılmamış.

### SORUN #17: TreatWarningsAsErrors = false
`Directory.Build.props`'da `TreatWarningsAsErrors` false. Nullable reference uyarıları build'i kırmıyor.

### SORUN #18: Dapper Connection Handling Tutarsız
- Query handler'lar: `using var connection = db.CreateConnection();` — OpenAsync çağırmıyor (Dapper auto-open yapar ama tutarsız)
- ClerkIdResolver: `await connection.OpenAsync()` — explicit açıyor

### SORUN #19: Search Wildcard Escape Yok
Dapper query'lerinde:
```sql
p.name ILIKE '%' || @Search || '%'
```
`%` ve `_` wildcard karakterleri escape edilmiyor. Kullanıcı `%` aratırsa tüm sonuçları alır.

### SORUN #20: Redis Integration Commented Out
`DependencyInjection.cs:60-68`'de Redis kodu yorum satırında. Her durumda `MemoryCacheService` kullanılıyor. Multi-instance deployment'ta cache tutarsızlığı olur.

---

## 6. DOĞRU ÇALIŞAN YAPILAR ✅

| Yapı | Durum | Not |
|------|-------|-----|
| Program.cs middleware sırası | ✅ | Auth → UserContext → Endpoints sırası doğru |
| MediatR behavior pipeline sırası | ✅ | Logging → Validation → UnitOfWork → Caching |
| DI registrations lifetime'ları | ✅ | Singleton/Scoped doğru kullanılmış |
| GlobalExceptionHandler | ✅ | ValidationException, DbUpdateConcurrencyException, OperationCanceledException handle ediliyor |
| Result pattern | ✅ | Error types doğru, HTTP status mapping doğru |
| Dapper SQL injection koruması | ✅ | Parametrized queries kullanılmış |
| JWT Authentication konfigürasyonu | ✅ | Issuer/lifetime validation, clock skew ayarlı |
| PermissionAuthorizationHandler | ✅ | Re-initialization guard, empty ID check var |
| Serilog konfigürasyonu | ✅ | Structured logging, log level override'lar doğru |
| Entity base class hierarchy | ✅ | Entity → OrganizationEntity → AggregateRoot mantıklı |
| ValueObject pattern (Money, DateRange) | ✅ | Immutable, equality doğru |
| Soft delete pattern | ✅ | ISoftDeletable + global filter (filter bug'ı hariç) |
| Audit fields otomasyonu | ✅ | SaveChangesAsync'de CreatedAt/UpdatedAt set ediliyor |
| Domain events pattern | ✅ | SaveChanges sonrası publish, ClearEvents |
| DbConnectionFactory | ✅ | Singleton, connection string validation var |
| ClerkIdResolver caching | ✅ | Memory cache ile 30dk, tuple caching |
| Resilience policies | ✅ | Retry, circuit breaker, timeout doğru konfigüre |
| PagedResult/PagedRequest | ✅ | Page min/max validation, TotalPages hesaplama |
| Error classes (per feature) | ✅ | Tutarlı naming, ErrorType doğru |

---

## 7. ÖNCELİK SIRASI — Ne Yapılmalı?

### İlk Yapılması Gerekenler (Feature'lardan ÖNCE)

1. **🔴 Global Filter Fix** — Multi-tenancy veri sızıntısını düzelt
2. **🔴 SaveChangesAsync** — Tüm command handler'lara ekle
3. **🔴 ClerkIdResolver** — Parallel query'yi sequential yap
4. **🟠 Validators** — En az create command'lar için yaz
5. **🟠 Lease Units/Parties** — CreateLeaseCommand'a ekle
6. **🟠 PropertyId → PublicId** — API'den PublicId kabul et, handler'da resolve et
7. **🟠 Permission SQL** — `p.is_active` kaldır veya Permission entity'ye ekle

### Feature'larla Paralel Yapılabilecekler

8. **🟡 CORS** ekle
9. **🟡 Health checks** ekle
10. **🟡 İlk migration** oluştur
11. **🟡 Temel testler** yaz (domain + handler)
12. **🟡 Update handler** null clear pattern'ini düzelt

---

## 8. MİMARİ DEĞERLENDİRME

### Güçlü Yönler
- Clean Architecture katmanları düzgün ayrılmış
- CQRS pattern doğru uygulanmış (Command → EF Core, Query → Dapper)
- Result pattern tutarlı, exception'a bağımlı değil
- Multi-tenancy altyapısı düşünülmüş (implementasyon buggy ama tasarım doğru)
- Permission-based authorization iyi tasarlanmış
- Domain model zengin (Property → Unit → Lease → Party ilişkileri)

### Zayıf Yönler
- 10/11 handler'da SaveChangesAsync çağrılmıyor — hiçbir write işlemi çalışmıyor
- Test coverage %0
- Migration yok — DB şeması oluşturulamaz
- Global filter güvenlik açığı
- Validator eksiklikleri

### Sonuç

Mimari ve tasarım pattern'leri doğru. Kodun yapısı, naming convention'lar, layer separation temiz. Ancak **implementasyon seviyesinde kritik hatalar** var — özellikle SaveChangesAsync eksikliği ve global filter bug'ı. Bu iki sorun düzeltilmeden hiçbir feature doğru çalışmaz.

Feature geliştirmeye başlamadan önce en az Sorun #1 (global filter), #2 (SaveChangesAsync), ve #3 (ClerkIdResolver) düzeltilmeli.
