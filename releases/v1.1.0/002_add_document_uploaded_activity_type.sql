ALTER TABLE activities DROP CONSTRAINT IF EXISTS ck_activities_activity_type;
ALTER TABLE activities ADD CONSTRAINT ck_activities_activity_type
   CHECK (activity_type IN ('note', 'phone_call', 'system_event', 'archived', 'unarchived', 'document_uploaded'));
