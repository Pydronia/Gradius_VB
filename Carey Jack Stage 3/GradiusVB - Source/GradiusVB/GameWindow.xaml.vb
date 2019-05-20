Public Class GameWindow

	Private gm As GameManager

	''' <summary>
	''' Window Constructor
	''' </summary>
	''' <param name="gm">The instance of the GameManager</param>
	Public Sub New(ByVal gm As GameManager)
		InitializeComponent()
		Me.gm = gm
	End Sub

	''' <summary>
	''' Setup game window
	''' </summary>
	Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
		gm.gw = Me
		lblName.Text = gm.getName()
		lblLives.Text = gm.getLives()
		lblHi.Text = gm.getHighScore().ToString("D7")
		gm.setup()
		gm.start()
	End Sub

#Region "KeyInputHandlers"

	''' <summary>
	''' Handle key input for gameplay. This is for detecting keyDown events
	''' </summary>
	Private Sub Window_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs) Handles MyBase.KeyDown
		If Not e.IsRepeat Then
			If (e.Key = Key.Up Or e.Key = Key.Down Or e.Key = Key.Left Or e.Key = Key.Right) Then
				gm.setInputDirection(True, e.Key)
			ElseIf e.Key = Key.A Then
				gm.prepareToShoot()
			ElseIf e.Key = Key.S Then
				gm.selectPowerUp()
			End If
		End If
	End Sub

	''' <summary>
	''' Handle key input for gameplay. This is for detecting keyUp Events
	''' </summary>
	Private Sub Window_KeyUp(sender As System.Object, e As System.Windows.Input.KeyEventArgs) Handles MyBase.KeyUp
		If Not e.IsRepeat And (e.Key = Key.Up Or e.Key = Key.Down Or e.Key = Key.Left Or e.Key = Key.Right) Then
			gm.setInputDirection(False, e.Key)
		End If
	End Sub

#End Region

	
End Class
