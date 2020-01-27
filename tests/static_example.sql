-- Here is an example static SQL query that makes use of a predefined SQL block
CREATE PROCEDURE [dbo].[_staticExample]
AS

SELECT
    ('[SqlBlock_
        MakeJsonIntArray(AU.Id,,AdminUsers AU (NOLOCK))
    _SqlBlock]') AS Integers
FROM AdminUsers AU (NOLOCK)