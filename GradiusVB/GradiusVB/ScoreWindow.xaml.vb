Public Class ScoreWindow

	Private gm As GameManager

	''' <summary>
	''' The constructor for the score window
	''' </summary>
	''' <param name="gm">The game manager</param>
	Public Sub New(ByVal gm As GameManager)
		InitializeComponent()
		Me.gm = gm
	End Sub

	''' <summary>
	''' Get scores, determine if the current score is a high score, display scores (after adding current score), and then store the new highscores array.
	''' </summary>
	Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		Dim highScores As HighScore()
		highScores = HighScoreManager.fetchHighScores()
		If highScores.Length = 0 Then
			lblHiGet.Visibility = Windows.Visibility.Visible
		ElseIf gm.getScore() > highScores(0).score Then
			lblHiGet.Visibility = Windows.Visibility.Visible
		End If
		

		Array.Resize(highScores, highScores.Length + 1)
		highScores(highScores.Length - 1) = New HighScore(gm.getScore(), gm.getName())
		HighScoreManager.sortHighScores(highScores)

		displayHighScores(highScores)
		HighScoreManager.writeHighScores(highScores)
	End Sub

	''' <summary>
	''' Add scores to listbox
	''' </summary>
	''' <param name="highScores">Scores to add</param>
	Private Sub displayHighScores(ByVal highScores As HighScore())
		For i = 0 To highScores.Length - 1
			lstHi.Items.Add(highScores(i).initials & ": " & highScores(i).score.ToString("D7"))
		Next i
	End Sub

	''' <summary>
	''' Go back to launchwindow
	''' </summary>
	Private Sub btnAgain_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnAgain.Click
		Dim lw As LaunchWindow
		lw = New LaunchWindow()
		lw.initials = gm.getName()
		lw.soundSetting = gm.getSoundSetting()
		lw.Show()
		Me.Close()
	End Sub
End Class
