-- Here is an example dynamic SQL query that makes use of a predefined SQL block
CREATE PROCEDURE [dbo].[_dynamicExample]
AS

DECLARE
    @SQL nvarchar(max) = ''

SET @SQL = @SQL +
    'SELECT
    (''[SqlBlock_
        MakeJsonIntArray(AU.Id,,AdminUsers AU (NOLOCK))
    _SqlBlock]'') AS Integers
    FROM AdminUsers AU (NOLOCK)'