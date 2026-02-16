ALTER TABLE contact_roles DROP CONSTRAINT IF EXISTS contact_roles_role_check;
ALTER TABLE contact_roles ADD CONSTRAINT contact_roles_role_check
   CHECK (role IN ('tenant', 'owner', 'supplier'));
DELETE FROM contact_roles WHERE role = 'vendor';
