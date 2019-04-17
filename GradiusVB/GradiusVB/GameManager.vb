Public Class GameManager

	Private name As String
	Private soundSetting As Boolean
	Private highScore As Integer

#Region "GameSpecific"

	Private lives As Integer


#End Region

#Region "Constructor/Getters"

	' Instance Constructor
	Public Sub New(ByVal name As String, ByVal soundSetting As Boolean)
		Me.name = name
		Me.soundSetting = soundSetting

		setup()
	End Sub

	Public Function getName() As String
		Return name
	End Function

	Public Function getLives() As Integer
		Return lives
	End Function

	Public Function getHighScore() As Integer
		Return highScore
	End Function

	' dummy/stub function!
	Public Function getDummyScore() As Integer
		Return 1337008
	End Function

	Public Function getSoundSetting() As Boolean
		Return soundSetting
	End Function

#End Region

	Private Sub setup()
		lives = 3
		highScore = HighScoreManager.getHighScore()
	End Sub

End Class
