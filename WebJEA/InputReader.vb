Public Class InputReader
    Private dlog As NLog.Logger = NLog.LogManager.GetCurrentClassLogger()

    Public Function GetParameters(cmd As PSCmd, page As Page, ByRef uinfo As UserInfo) As Dictionary(Of String, Object)
        Dim params As New Dictionary(Of String, Object)

        If Not (cmd.Parameters Is Nothing) Then
            For Each param As PSCmdParam In cmd.Parameters
                Dim ctrl As WebControl = CType(page.FindControl(param.FieldName), WebControl)

                If (param.Name.ToUpper().StartsWith("WEBJEA")) Then
                    GetParameterInternal(param, page, params, uinfo)
                ElseIf (param.IsSelect) Then
                    GetParameterSelect(param, page, params)
                ElseIf param.ParamType = PSCmdParam.ParameterType.PSBoolean Then
                    GetParameterCheckbox(param, page, params)
                ElseIf (param.ParamType = PSCmdParam.ParameterType.PSString) Then
                    GetParameterString(param, page, params)
                ElseIf (param.ParamType = PSCmdParam.ParameterType.PSFloat) Then
                    GetParameterString(param, page, params)
                ElseIf (param.ParamType = PSCmdParam.ParameterType.PSInt) Then
                    GetParameterString(param, page, params)
                ElseIf (param.ParamType = PSCmdParam.ParameterType.PSDate) Then
                    GetParameterString(param, page, params)
                Else
                    dlog.Warn("Processing: " & param.Name & " as string, type (" & param.VarType & ") not expected")
                    GetParameterString(param, page, params)
                End If

            Next
        End If

        Return params
    End Function

    Public Function GetVerboseParameter(page As Page) As Boolean
        Dim ctrl As CheckBox = page.FindControl("chkWebJEAVerbose")
        If ctrl IsNot Nothing Then
            Return ctrl.Checked
        End If
        Return False
    End Function

    Private Sub GetParameterInternal(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object), ByRef uinfo As UserInfo)
        If param.Name.ToUpper() = "WEBJEAUSERNAME" Then
            params.Add(param.Name, uinfo.UserName)
        ElseIf param.Name.ToUpper() = "WEBJEAHOSTNAME" Then
            params.Add(param.Name, page.Request.UserHostName)
        Else
            dlog.Warn("Parameter Name '" & param.Name & "' is not a recognized internal parameter and cannot be used.  Parameters with WEBJEA prefixes are reserved.")
        End If
    End Sub

    Private Sub GetParameterSelect(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))
        If param.IsMultiValued Then
            GetParameterListbox(param, page, params)
        Else
            GetParameterDropdown(param, page, params)
        End If
    End Sub

    Private Sub GetParameterListbox(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))

        Dim ctrl As ListBox = page.FindControl(param.FieldName)
        If ctrl.GetSelectedIndices.Count > 0 Then

            Dim valset As New List(Of String)
            For Each idx As Integer In ctrl.GetSelectedIndices.ToList
                If ctrl.Items(idx).Value <> "" And ctrl.Items(idx).Value <> "--Select--" Then
                    valset.Add(ctrl.Items(idx).Value)
                End If
            Next
            params.Add(param.Name, valset.ToArray)

        End If

    End Sub

    Private Sub GetParameterDropdown(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))

        Dim ctrl As DropDownList = page.FindControl(param.FieldName)
        If ctrl.SelectedValue <> "" And ctrl.SelectedValue <> "--Select--" Then
            params.Add(param.Name, ctrl.SelectedValue)
        End If

    End Sub

    Private Sub GetParameterString(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))

        If param.IsMultiValued Then
            GetParameterStringSet(param, page, params)
        Else
            GetParameterStringSingle(param, page, params)
        End If

    End Sub

    Private Sub GetParameterStringSingle(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))

        Dim ctrl As TextBox = page.FindControl(param.FieldName)
        If ctrl.Text <> "" Then
            params.Add(param.Name, ctrl.Text)
        End If

    End Sub

    Private Sub GetParameterStringSet(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))
        Dim trimchar As Char() = New Char() {vbCr, vbLf}
        Dim ctrl As TextBox = page.FindControl(param.FieldName)

        Dim strarr As String() = ctrl.Text.Split(vbLf)
        Dim strlist As New List(Of String)
        For Each item As String In strarr
            If item.Trim(trimchar).Trim <> "" Then
                strlist.Add(item.Trim(trimchar).Trim)
            End If
        Next
        If strlist.Count > 0 Then
            params.Add(param.Name, strlist.ToArray)
        End If

    End Sub

    Private Sub GetParameterCheckbox(param As PSCmdParam, page As Page, ByRef params As Dictionary(Of String, Object))

        Dim ctrl As CheckBox = page.FindControl(param.FieldName)
        params.Add(param.Name, ctrl.Checked)

    End Sub

End Class
