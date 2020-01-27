-- Here is an example of combining static and dynamic SQL queries that both make use of a predefined SQL block
CREATE PROCEDURE [dbo].[_compoundExample]
AS

DECLARE
    @SQL nvarchar(max) = ''

SET @SQL = @SQL +
    'SELECT
    (''[SqlBlock_
        MakeJsonIntArray(AU.Id,,AdminUsers AU (NOLOCK))
    _SqlBlock]'') AS Integers
FROM AdminUsers AU (NOLOCK)'

SELECT
    ('[SqlBlock_
        MakeJsonIntArray(AU.Id,,AdminUsers AU (NOLOCK))
    _SqlBlock]') AS Integers
FROM AdminUsers AU (NOLOCK)