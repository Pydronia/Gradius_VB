Public Class HelpWindow

	''' <summary>
	''' Show the interactive tutorial
	''' </summary>
	Private Sub btnStart_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnStart.Click
		Process.Start(AppDomain.CurrentDomain.BaseDirectory & "/Tutorial/Gradius_Tutorial.ppsx")
	End Sub
End Class
