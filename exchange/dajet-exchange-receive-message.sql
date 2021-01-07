WITH [CTE] AS
    (
        SELECT TOP(1)
            [_Fld83] AS [ТипОперации],
            [_Fld84] AS [ТипСообщения],
            [_Fld85] AS [ТелоСообщения]
        FROM
            [dbo].[_Reference81] WITH(rowlock)
        ORDER BY
            [_Code] ASC, [_IDRRef] ASC
    )
    DELETE
        [CTE]
    OUTPUT
        deleted.[ТипОперации], deleted.[ТипСообщения], deleted.[ТелоСообщения];