// Global using directives for Propentro.Application
// These imports are available in all files within this project

// CQRS / Messaging
global using Propentro.Application.Common.Interfaces.Messaging;

// Persistence
global using Propentro.Application.Common.Interfaces.Persistence;

// Identity

// Caching
global using Propentro.Application.Common.Interfaces.Caching;

// Storage

// Time
global using Propentro.Application.Common.Interfaces.Time;

// Domain Entities (feature-based sub-namespaces)
global using Propentro.Domain.Entities.Addresses;
global using Propentro.Domain.Entities.Contacts;
global using Propentro.Domain.Entities.Documents;
global using Propentro.Domain.Entities.Identity;
global using Propentro.Domain.Entities.Lookups;
global using Propentro.Domain.Entities.Notifications;
global using Propentro.Domain.Entities.Outbox;
global using Propentro.Domain.Entities.Properties;
global using Propentro.Domain.Entities.Units;
