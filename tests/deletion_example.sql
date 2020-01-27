-- Here is an example static SQL query that makes use of a predefined SQL block
CREATE PROCEDURE [dbo].[_deletionExample]
(
	@SiteUserID bigint,
	@AdminUserIDs nvarchar(max)
)
AS

BEGIN TRANSACTION;

BEGIN TRY

SET NOCOUNT ON;

	----------------------------------Validation------------------------------------------

	DECLARE
		@ValidationMessage nvarchar(MAX) = '',
		@JsonData nvarchar(MAX) = ''
	DECLARE
		@LookupName nvarchar(100) = ''
	DECLARE
		@LookupSiUID bigint

	SELECT
		 @LookupName = CONCAT(CD.FirstName, ' ', CD.LastName)
	FROM AdminUsers AU (NOLOCK)	
	JOIN ContactDetails CD (NOLOCK) ON CD.SiteUserID = AU.SiteUserID
	WHERE AU.SiteUserID = @SiteUserID
	  AND AU.bSuppressed = 0 AND AU.bDeleted = 0

	IF (@@ROWCOUNT <> 1)
	BEGIN            
		SET @ValidationMessage =  [dbo].[v1_fn_General_AddValidationMessage](@ValidationMessage , 'Error_Label', 'Invalid_Access'); 
	END

	IF(@ValidationMessage IS NULL OR @ValidationMessage = '')
	BEGIN

	----------------------------------Get IDs table------------------------------------------

	IF OBJECT_ID('tempdb..#IDs') IS NOT NULL
		DROP TABLE #IDs

	CREATE TABLE #IDs (
		ID bigint
	)

	WHILE (CHARINDEX(',', @AdminUserIDs) > 0)
	BEGIN
		INSERT INTO #IDs (ID)
		SELECT 
			LTRIM(
				RTRIM(
					SUBSTRING(@AdminUserIDs, 1, CHARINDEX(',', @AdminUserIDs) - 1)
				)
			)
 
		SET @AdminUserIDs = SUBSTRING(@AdminUserIDs, CHARINDEX(',', @AdminUserIDs) + 1, LEN(@AdminUserIDs))
	END

	----------------------------------Validate deletions------------------------------------------

	IF OBJECT_ID('tempdb..#Deletions') IS NOT NULL
		DROP TABLE #Deletions

	CREATE TABLE #Deletions (
		ID bigint NOT NULL,
		ErrorMessage nvarchar(MAX) NULL
	)

	INSERT INTO #Deletions (
		ID,
		ErrorMessage
	)
	SELECT
		IDs.ID,
		CASE
			WHEN (AU.Id IS NULL) OR (AU.bDeleted = 1)
			THEN 'Admin User does not exist'
			ELSE NULL
		END AS ErrorMessage
	FROM #IDs IDs (NOLOCK)
	LEFT JOIN AdminUsers AU (NOLOCK) ON AU.Id = IDs.ID

	----------------------------------Delete Admin Users------------------------------------------

	IF OBJECT_ID('tempdb..#IDsToDelete') IS NOT NULL
		DROP TABLE #IDsToDelete

	CREATE TABLE #IDsToDelete (
		AdminUserID bigint,
		SiteUserID bigint
	)

	INSERT INTO #IDsToDelete (
		AdminUserID,
		SiteUserID
	)
	SELECT
		AU.Id AS AdminUserID,
		AU.SiteUserID AS SiteUserID
	FROM AdminUsers AU (NOLOCK)
	JOIN #Deletions Dels (NOLOCK) ON Dels.ID = AU.Id
	WHERE Dels.ErrorMessage IS NULL

	UPDATE AdminUsers
	SET
		bDeleted = 1,
		EditDate = GETUTCDATE(),
		LastEditedBy = @LookupName
	WHERE Id IN
		(SELECT AdminUserID
		FROM #IDsToDelete (NOLOCK))

	----------------------------------Delete Site User------------------------------------------

	UPDATE SiteUsers
	SET
		bDeleted = 1
	WHERE Id IN
		(SELECT SiteUserID
		FROM #IDsToDelete (NOLOCK))

	----------------------------------Select deleted Admin Users------------------------------------------

	SELECT
		@JsonData = 
		('[SqlBlock_
			SelectDeletions()
		_SqlBlock]')

	END

	SELECT
		CASE
			WHEN @ValidationMessage IS NOT NULL AND @ValidationMessage <> ''
			THEN 0
			ELSE 1
		END AS [Status],
		NULL AS Id,
		@JsonData AS Details,
		@ValidationMessage AS Errors

SET NOCOUNT OFF;

COMMIT TRANSACTION;

END TRY
BEGIN CATCH

  ROLLBACK TRANSACTION;
  EXEC _throwException;

END CATCH