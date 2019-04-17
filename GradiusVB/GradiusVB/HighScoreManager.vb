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

	' Return the highest score. This is called at the start of every game and will handle the creation of a new file as well (if its not there)
	Public Function getHighScore() As Integer
		Dim score As Integer
		If Not fileExists() Then
			createFile()
			score = 0
		Else
			Dim scores As HighScore()
			scores = fetchHighScores()
			If scores.Length = 0 Then
				score = 0
			Else
				score = scores(0).score
			End If
		End If
		Return score
	End Function

	' grab and return all (sorted) highscores
	Public Function fetchHighScores() As HighScore()
		Dim highScores(-1) As HighScore

		Using sr As New StreamReader(directory & fileName)
			While Not sr.EndOfStream
				Dim line As String
				line = sr.ReadLine()
				If line <> "" Then
					Dim stringSplit As String()
					stringSplit = line.Split(":")

					Array.Resize(highScores, highScores.Length + 1)
					highScores(highScores.Length - 1) = New HighScore(CInt(stringSplit(1)), stringSplit(0))
				End If
			End While
		End Using

		sortHighScores(highScores)
		Return highScores
	End Function

	' decending insertion sort (as the highscores will usually be very close to sorted)
	Public Sub sortHighScores(ByRef highScores As HighScore())
		Dim first As Integer = 0
		Dim last As Integer = highScores.Length - 1
		Dim positionOfNext As Integer = last - 1
		While positionOfNext >= first
			Dim temp As HighScore = highScores(positionOfNext)
			Dim currentPos As Integer = positionOfNext
			While (currentPos < last) AndAlso (temp.score < highScores(currentPos + 1).score)
				highScores(currentPos) = highScores(currentPos + 1)
				currentPos = currentPos + 1
			End While
			highScores(currentPos) = temp
			positionOfNext = positionOfNext - 1
		End While
	End Sub

	' write highscores to file
	Public Sub writeHighScores(ByVal highScores As HighScore())
		Using sw As New StreamWriter(directory & fileName)
			For i = 0 To highScores.Length - 1
				sw.WriteLine(highScores(i).initials & ":" & CStr(highScores(i).score))
			Next i
		End Using
	End Sub

	' Check if the file exists
	Private Function fileExists() As Boolean
		Return File.Exists(directory & fileName)
	End Function

	' Create the file and/or directory.
	Private Sub createFile()
		If Not System.IO.Directory.Exists(directory) Then
			System.IO.Directory.CreateDirectory(directory)
		End If
		Dim fs As FileStream = File.Create(directory & fileName)
		fs.Close()
	End Sub


End Module
