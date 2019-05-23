Class LaunchWindow

	Public soundSetting As Boolean
	Public initials As String
	Private timer As System.Windows.Threading.DispatcherTimer

#Region "EventHandlers"

	''' <summary>
	''' This event is for the initial input box, and blocks the user from inputting anything other than uppercase letters.
	''' This simplifies the name validation. It also handles the user pressing enter, and calls the checkStart method.
	''' </summary>
	Private Sub initialInput_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs) Handles initialInput.KeyDown
		If e.Key < Key.A Or e.Key > Key.Z Then
			e.Handled = True
		ElseIf e.Key = Key.Enter Or e.Key = Key.Return Then
			checkStart()
		End If
	End Sub


	''' <summary>
	''' This event begins the start process when the start button is clicked
	''' </summary>
	Private Sub btnStart_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnStart.Click
		checkStart()
	End Sub

	''' <summary>
	''' Toggle the sound setting
	''' </summary>
	Private Sub btnSound_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnSound.Click
		If soundSetting = False Then
			soundSetting = True
			btnSound.Content = "SOUND: ON"
		Else
			soundSetting = False
			btnSound.Content = "SOUND: OFF"
		End If
	End Sub

	''' <summary>
	''' show the help screen!
	''' </summary>
	Private Sub btnHelp_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles btnHelp.Click
		Dim helpWin As HelpWindow
		helpWin = New HelpWindow()
		helpWin.ShowDialog()
	End Sub

#End Region

	''' <summary>
	''' set the sound setting and initials to carry over from endgame
	''' </summary>
	Private Sub Window_Loaded(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		' Carry over from the last game, setting sound and initials.
		If soundSetting = False Then
			btnSound.Content = "SOUND: OFF"
		Else
			btnSound.Content = "SOUND: ON"
		End If

		initialInput.Text = initials
	End Sub

	''' <summary>
	''' Name validation, with user alert
	''' </summary>
	''' <param name="name">The name to check</param>
	''' <returns>Boolean, True if name is valid</returns>
	Private Function isNameValid(ByVal name As String) As Boolean
		Dim valid As Boolean = True
		For i = 0 To name.Length - 1
			' Ensure character is a capital letter.
			If Asc(name(i)) < Asc("A") Or Asc(name(i)) > Asc("Z") Then
				valid = False
			End If
		Next i
		If Not valid Then
			btnStart.Content = "USE ONLY UPPERCASE LETTERS"
			btnStart.FontSize = 18
			btnStart.IsEnabled = False
		ElseIf name.Length <> 3 Then
			' make sure it's three letters long
			btnStart.FontSize = 18
			btnStart.Content = "ENTER THREE LETTERS"
			btnStart.IsEnabled = False
		End If
		Return (valid And (name.Length = 3))
	End Function

	''' <summary>
	''' Method to start the initialization of the GameManager and hence the game
	''' </summary>
	Private Sub checkStart()
		If isNameValid(initialInput.Text) Then
			' good to start!
			startGame()
		Else
			' invalid! clear the textbox.
			initialInput.Text = ""
			timer = New System.Windows.Threading.DispatcherTimer()
			AddHandler timer.Tick, AddressOf timer_Tick
			timer.Interval = New TimeSpan(0, 0, 0, 1, 200)
			timer.Start()
		End If
	End Sub

	''' <summary>
	''' Switch the button label back to normal after alert
	''' </summary>
	Private Sub timer_Tick(ByVal sender As Object, ByVal e As EventArgs)
		' Reset the button text back to normal
		btnStart.Content = "PLAYER 1 START"
		btnStart.FontSize = 24
		btnStart.IsEnabled = True
		timer.Stop()
	End Sub

	''' <summary>
	''' Start the game!
	''' </summary>
	Private Sub startGame()
		' make the game manager, game window, and show the next window.
		Dim gm As GameManager
		gm = New GameManager(initialInput.Text, soundSetting)
		Dim gw As GameWindow
		gw = New GameWindow(gm)
		gm.sm.playSoundEffect(SoundEffects.Start)
		gw.Show()
		Me.Close()
	End Sub

End Class
