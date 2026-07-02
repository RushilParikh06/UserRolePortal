-- =============================================
-- PostgreSQL Stored Procedures (Functions)
-- Execute this script to create the database procedures
-- =============================================

-- 1. sp_get_dashboard_stats
DROP FUNCTION IF EXISTS sp_get_dashboard_stats();
CREATE OR REPLACE FUNCTION sp_get_dashboard_stats()
RETURNS TABLE (TotalUsers bigint, SuperAdminCount bigint, AdminCount bigint, UserCount bigint)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY 
    SELECT 
        COUNT(1) AS TotalUsers,
        SUM(CASE WHEN "RoleId" = 3 THEN 1 ELSE 0 END) AS SuperAdminCount,
        SUM(CASE WHEN "RoleId" = 1 THEN 1 ELSE 0 END) AS AdminCount,
        SUM(CASE WHEN "RoleId" = 2 THEN 1 ELSE 0 END) AS UserCount
    FROM "Users";
END;
$$;

-- 2. sp_get_recent_users
CREATE OR REPLACE FUNCTION sp_get_recent_users()
RETURNS SETOF "Users"
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM "Users" ORDER BY "CreatedDate" DESC LIMIT 5;
END;
$$;

-- 3. sp_get_all_users_with_roles
DROP FUNCTION IF EXISTS sp_get_all_users_with_roles();
CREATE OR REPLACE FUNCTION sp_get_all_users_with_roles()
RETURNS TABLE (
    "UserId" integer, "FullName" character varying(100), "Username" character varying(50), "Password" character varying(100), 
    "Email" character varying(50), "MobileNo" character varying(10), "DOB" timestamp with time zone, 
    "RoleId" integer, "Gender" text, "CreatedDate" timestamp with time zone, "Status" integer, "StatusReason" text,
    "Role_RoleId" integer, "RoleName" text
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY 
    SELECT u."UserId", u."FullName", u."Username", u."Password", u."Email", 
           u."MobileNo", u."DOB", u."RoleId", u."Gender", u."CreatedDate", u."Status", u."StatusReason",
           r."RoleId" AS "Role_RoleId", r."RoleName"
    FROM "Users" u
    LEFT JOIN "Roles" r ON u."RoleId" = r."RoleId"
    ORDER BY u."CreatedDate" DESC;
END;
$$;

-- 4. sp_update_user
CREATE OR REPLACE FUNCTION sp_update_user(
    p_userid integer, p_fullname text, p_email text, p_mobileno text, p_roleid integer
) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE "Users" 
    SET "FullName" = p_fullname, "Email" = p_email, "MobileNo" = p_mobileno, "RoleId" = p_roleid 
    WHERE "UserId" = p_userid;
END;
$$;

-- 5. sp_delete_user
CREATE OR REPLACE FUNCTION sp_delete_user(p_userid integer) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM "Users" WHERE "UserId" = p_userid;
END;
$$;

-- 6. sp_get_documents
DROP FUNCTION IF EXISTS sp_get_documents(integer);
CREATE OR REPLACE FUNCTION sp_get_documents(p_userid integer DEFAULT NULL)
RETURNS TABLE (
    "DocumentId" integer, "FileName" character varying(255), "FilePath" character varying(500), "UploadedDate" timestamp with time zone, 
    "UserId" integer, "DocumentType" text, "StatusId" integer, "RejectionReason" character varying(1000),
    "User_UserId" integer, "FullName" character varying(100), "Username" character varying(50), "Password" character varying(100), 
    "Email" character varying(50), "MobileNo" character varying(10), "DOB" timestamp with time zone, 
    "RoleId" integer, "Gender" text, "CreatedDate" timestamp with time zone, "Status" integer, "StatusReason" text
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_userid IS NOT NULL THEN
        RETURN QUERY 
        SELECT d."DocumentId", d."FileName", d."FilePath", d."UploadedDate", 
               d."UserId", d."DocumentType", d."StatusId", d."RejectionReason",
               u."UserId" AS "User_UserId", u."FullName", u."Username", u."Password", u."Email", 
               u."MobileNo", u."DOB", u."RoleId", u."Gender", u."CreatedDate", u."Status", u."StatusReason"
        FROM "Documents" d 
        LEFT JOIN "Users" u ON d."UserId" = u."UserId" 
        WHERE d."UserId" = p_userid 
        ORDER BY d."UploadedDate" DESC;
    ELSE
        RETURN QUERY 
        SELECT d."DocumentId", d."FileName", d."FilePath", d."UploadedDate", 
               d."UserId", d."DocumentType", d."StatusId", d."RejectionReason",
               u."UserId" AS "User_UserId", u."FullName", u."Username", u."Password", u."Email", 
               u."MobileNo", u."DOB", u."RoleId", u."Gender", u."CreatedDate", u."Status", u."StatusReason"
        FROM "Documents" d 
        LEFT JOIN "Users" u ON d."UserId" = u."UserId" 
        ORDER BY d."UploadedDate" DESC;
    END IF;
END;
$$;

-- 7. sp_insert_document
CREATE OR REPLACE FUNCTION sp_insert_document(
    p_filename text, p_filepath text, p_uploadeddate timestamp with time zone, p_userid integer, p_documenttype text
) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- StatusId 1 = Pending by default
    INSERT INTO "Documents" ("FileName", "FilePath", "UploadedDate", "UserId", "StatusId", "DocumentType") 
    VALUES (p_filename, p_filepath, p_uploadeddate, p_userid, 1, p_documenttype);
END;
$$;

-- 8. sp_get_document_for_deletion
CREATE OR REPLACE FUNCTION sp_get_document_for_deletion(p_documentid integer)
RETURNS TABLE ("FilePath" text, "StatusId" integer, "UserId" integer)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT d."FilePath"::text, d."StatusId", d."UserId" FROM "Documents" d WHERE d."DocumentId" = p_documentid;
END;
$$;

-- 9. sp_delete_document
CREATE OR REPLACE FUNCTION sp_delete_document(p_documentid integer) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    DELETE FROM "Documents" WHERE "DocumentId" = p_documentid;
END;
$$;

-- 10. sp_verify_document
CREATE OR REPLACE FUNCTION sp_verify_document(p_documentid integer) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    -- StatusId 2 = Verified
    UPDATE "Documents" SET "StatusId" = 2, "RejectionReason" = NULL WHERE "DocumentId" = p_documentid;
END;
$$;

-- 10b. sp_reject_document  (StatusId 3 = Rejected, reason is mandatory)
CREATE OR REPLACE FUNCTION sp_reject_document(p_documentid integer, p_reason text) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_reason IS NULL OR TRIM(p_reason) = '' THEN
        RAISE EXCEPTION 'Rejection reason is mandatory.';
    END IF;
    -- StatusId 3 = Rejected
    UPDATE "Documents" SET "StatusId" = 3, "RejectionReason" = TRIM(p_reason) WHERE "DocumentId" = p_documentid;
END;
$$;

-- 11. sp_check_username_exists
CREATE OR REPLACE FUNCTION sp_check_username_exists(p_username text)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    res bigint;
BEGIN
    SELECT COUNT(1) INTO res FROM "Users" WHERE "Username" = p_username;
    RETURN res;
END;
$$;

-- 12. sp_check_email_exists
CREATE OR REPLACE FUNCTION sp_check_email_exists(p_email text)
RETURNS bigint
LANGUAGE plpgsql
AS $$
DECLARE
    res bigint;
BEGIN
    SELECT COUNT(1) INTO res FROM "Users" WHERE "Email" = p_email;
    RETURN res;
END;
$$;

-- 13. sp_insert_user
CREATE OR REPLACE FUNCTION sp_insert_user(
    p_fullname text, p_username text, p_password text, p_email text, p_mobileno text, 
    p_dob timestamp with time zone, p_roleid integer, p_gender text, p_createddate timestamp with time zone,
    p_status integer, p_statusreason text
) RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO "Users" ("FullName", "Username", "Password", "Email", "MobileNo", "DOB", "RoleId", "Gender", "CreatedDate", "Status", "StatusReason") 
    VALUES (p_fullname, p_username, p_password, p_email, p_mobileno, p_dob, p_roleid, p_gender, p_createddate, p_status, p_statusreason);
END;
$$;

-- 14. sp_get_user_by_username
CREATE OR REPLACE FUNCTION sp_get_user_by_username(p_username text)
RETURNS SETOF "Users"
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM "Users" WHERE "Username" = p_username;
END;
$$;

-- 15. sp_update_user_status
CREATE OR REPLACE FUNCTION sp_update_user_status(
    p_userid integer, p_newstatus integer, p_reason text, p_changedate timestamp with time zone
) RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
    v_oldstatus integer;
BEGIN
    SELECT "Status" INTO v_oldstatus FROM "Users" WHERE "UserId" = p_userid;
    
    UPDATE "Users" 
    SET "Status" = p_newstatus, "StatusReason" = p_reason 
    WHERE "UserId" = p_userid;

    INSERT INTO "UserStatusHistories" ("UserId", "PreviousStatus", "NewStatus", "Reason", "ChangeDate")
    VALUES (p_userid, v_oldstatus, p_newstatus, p_reason, p_changedate);
END;
$$;

-- Seed SuperAdmin role if it doesn't exist
INSERT INTO "Roles" ("RoleId", "RoleName")
SELECT 3, 'SuperAdmin'
WHERE NOT EXISTS (SELECT 1 FROM "Roles" WHERE "RoleId" = 3);
