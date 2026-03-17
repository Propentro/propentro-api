# Webhooks

## POST /v1/webhooks/clerk — organizationMembership.created

Processes new organization membership events from Clerk.

**Auth:** Svix signature verification (AllowAnonymous)

**Business Logic:**
1. Verify Svix webhook signature
2. Parse webhook payload
3. Check idempotency (skip if already processed)
4. Upsert user from Clerk external ID (create if new, update if existing)
5. Resolve organization by Clerk external ID
6. Guard: organization not found → skip silently (log warning)
7. Guard: membership already exists → skip silently (log info)
8. Find pending invite for the user's email in this organization
9. Guard: no pending invite found → skip silently (log warning)
10. Create organization membership with status Active
11. Assign role from invite
12. Mark invite as Accepted
13. Add outbox event

**Events:** `InviteAcceptedEvent`

**Response:** 200 OK | 400 / 401 / 500
