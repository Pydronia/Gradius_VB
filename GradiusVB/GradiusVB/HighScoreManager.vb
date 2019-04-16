Imports System.IO

Module HighScoreManager

	' structure for storing initials with scores
	Public Structure HighScore
		Public score As Integer
		Public initials As String

		' constructor
		Public Sub New(ByVal inScore As Integer, ByVal inInitials As String)
			score = inScore
			initials = inInitials
		End Sub
	End Structure

	Private directory As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create) & "\GradiusVB\"
	Private fileName As String = "gradius-highscores.txt"

	Public Function getHighScore() As Integer
		Dim score As Integer
		If Not fileExists() Then
			createFile()
			score = 0
		Else
			score = fetchHighScores()(0).score
		End If
		Return score
	End Function

	Private Function fetchHighScores() As HighScore()
		' TODO here
	End Function

	Private Function fileExists() As Boolean
		Return File.Exists(directory & fileName)
	End Function

	Private Sub createFile()
		File.Create(directory & fileName)
	End Sub


End Module
