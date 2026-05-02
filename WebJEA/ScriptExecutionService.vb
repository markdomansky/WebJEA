Public Class ScriptExecutionService

    Public Function Execute(script As String,
                           parameters As Dictionary(Of String, Object),
                           logParameters As Boolean,
                           webjeaUserName As String,
                           webjeaHostName As String,
                           Optional verbose As Boolean = False,
                           Optional pipeToOutString As Boolean = True) As IScriptEngine
        Dim ps As IScriptEngine = New PSEngine
        ps.Script = script
        ps.LogParameters = logParameters
        ps.Parameters = parameters
        ps.Verbose = verbose
        ps.PipeToOutString = pipeToOutString
        ps.WebJEAUserName = webjeaUserName
        ps.WebJEAHostName = webjeaHostName
        ps.Run()
        Return ps
    End Function

End Class
