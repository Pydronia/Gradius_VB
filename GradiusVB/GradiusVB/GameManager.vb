﻿' Structure to represent directions
Public Structure Directions
	Public up As Boolean
	Public down As Boolean
	Public left As Boolean
	Public right As Boolean

	Public Shared Operator =(ByVal d1 As Directions, ByVal d2 As Directions) As Boolean
		Return (d1.up = d2.up And d1.down = d2.down And d1.left = d2.left And d1.right = d2.right)
	End Operator
	Public Shared Operator <>(ByVal d1 As Directions, ByVal d2 As Directions) As Boolean
		Return Not (d1.up = d2.up And d1.down = d2.down And d1.left = d2.left And d1.right = d2.right)
	End Operator
End Structure

' enum to represent what animation frame to display
Public Enum AnimationFrame
	Neutral
	Up
	Down
End Enum

' Class for storing the representation of maps and terrain
Public Class Map

	Const mapMoveSpeed As Integer = 64

	Public control As Image

	Public collisionMap As Boolean(,)

	Public length As Integer
	Public position As Double
	Public movementSpeed As Double

	Public Sub New(ByVal control As Image, ByVal collisionImage As BitmapImage)
		Me.control = control
		Me.length = control.Width
		Me.position = 0
		Me.movementSpeed = mapMoveSpeed

		Canvas.SetZIndex(Me.control, 2)

		' generating collision array
		' pixel data generation
		Dim stride As Integer = collisionImage.PixelWidth * collisionImage.Format.BitsPerPixel / 8
		Dim pixelArray((collisionImage.PixelHeight * stride) - 1) As Byte
		collisionImage.CopyPixels(pixelArray, stride, 0)

		collisionMap = New Boolean(collisionImage.PixelHeight - 1, collisionImage.PixelWidth - 1) {}

		' collisionmap generation (boolean array)
		Dim counter As Integer = 0
		For i = 0 To collisionImage.PixelHeight - 1
			For j = 0 To collisionImage.PixelWidth - 1
				If CInt(pixelArray(counter)) > 0 Then
					collisionMap(i, j) = False
				Else
					collisionMap(i, j) = True
				End If
				counter = counter + 4
			Next j
		Next i

	End Sub

	Public Sub ui_updateMapPosition()
		Canvas.SetLeft(control, position)
	End Sub

End Class

' Overall class for entities, including bullets, enemies, and the player
Public Class Entity
	Public movementDirection As Vector
	Public position As Point
	Public movementSpeed As Double
	Public hitBox As Rect
	Public freeDirections As Directions

	Public currentAnimationFrame As AnimationFrame
	Public animationFrames As BitmapImage()

	Public deathFrameNum As Integer
	Public deathAnimationFrames As BitmapImage()
	Public isDying As Boolean = False
	Public deathAnimationDelay As Integer
	Public deathAnimationDelayInterval As DateTime
	Public deathAnimationDelayStart As DateTime


	Public type As String
	Public control As Image

	Public Sub New(ByVal type As String, ByVal control As Image, ByVal position As Point, ByVal movementSpeed As Double, ByVal deathDelay As Integer)
		Me.type = type
		Me.control = control
		Me.position = position
		Me.movementSpeed = movementSpeed
		Me.deathAnimationDelay = deathDelay
		Me.deathFrameNum = -1
		Me.hitBox = New Rect(position.X, position.Y, control.Width, control.Height)

		currentAnimationFrame = AnimationFrame.Neutral

		freeDirections.up = True
		freeDirections.down = True
		freeDirections.left = True
		freeDirections.right = True
	End Sub

	' position update
	Public Sub ui_updatePosition()
		Canvas.SetLeft(Control, position.X)
		Canvas.SetTop(Control, position.Y)
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

	Public Sub ui_updateFrame()
		If Me.isDying Then
			If deathFrameNum >= deathAnimationFrames.Length Then
				control.Source = Nothing
			Else
				control.Source = deathAnimationFrames(deathFrameNum)
			End If
		Else
			control.Source = animationFrames(currentAnimationFrame)
		End If

	End Sub
End Class

' The class for the player entity
Public Class VicViper
	Inherits Entity

	Public Const startingSpeed As Integer = 128
	Public Const startingPosX As Double = 50
	Public Const startingPosY As Double = 200
	Public Const vicResetDelay As Integer = 2000

	Private Const vicDeathDelayTime As Integer = 250


	Public directionKeys As Directions

	Public Sub New(ByVal control As Image)
		MyBase.New("vic", control, New Point(startingPosX, startingPosY), startingSpeed, vicDeathDelayTime)

		Canvas.SetZIndex(Me.control, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/vicViper.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Up.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Down.png")}
		deathAnimationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/vicViper_Death_0.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_1.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_2.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_3.png")}
	End Sub

	Public Sub resetState()
		isDying = False
		position = New Point(startingPosX, startingPosY)
		movementDirection = New Vector(0, 0)
		movementSpeed = startingSpeed
		currentAnimationFrame = AnimationFrame.Neutral
		deathFrameNum = -1
	End Sub

End Class


Public Class GameManager

	Public gw As GameWindow

	Private name As String
	Private soundSetting As Boolean
	Private highScore As Integer

#Region "GameSpecific"

	Private scaleFactor As Integer = 2
	Const gameWidth As Integer = 256
	Const gameHeight As Integer = 208
	Const vicAnimationDelay As Integer = 100

	Public vicViper As VicViper

	Public map As Map

	Private lives As Integer

	Private gameTimer As Timers.Timer
	Private previousTime As DateTime
	Private deltaLoopTime As TimeSpan

	Private vicAnimationTicking As Boolean
	Private vicAnimationDelayStart As DateTime

	Private shallReset As Boolean = False


#End Region

#Region "Delegates"
	Private Delegate Sub InvokeDelegate()
#End Region

#Region "Constructor/Getters"

	' Instance Constructor
	Public Sub New(ByVal name As String, ByVal soundSetting As Boolean)
		Me.name = name
		Me.soundSetting = soundSetting
		preSetup()
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

#Region "Input Handling"

	' Set the direction of the vic viper
	Public Sub setInputDirection(ByVal press As Boolean, ByVal key As Key)
		Select Case key
			Case Input.Key.Up
				vicViper.directionKeys.up = press
			Case Input.Key.Down
				vicViper.directionKeys.down = press
			Case Input.Key.Left
				vicViper.directionKeys.left = press
			Case Input.Key.Right
				vicViper.directionKeys.right = press
		End Select

		updateVicVector()

	End Sub
#End Region

#Region "Initialisation/Reset Routines"

	' setup before game window created
	Private Sub preSetup()
		lives = 3
		highScore = HighScoreManager.getHighScore()
	End Sub

	' setup when game window created
	Public Sub setup()
		previousTime = DateTime.Now

		vicViper = New VicViper(makeNewSprite("/Images/vicViper.png"))
		vicViper.ui_updatePosition()
		gw.gameField.Children.Add(vicViper.control)

		map = New Map(makeNewSprite("/Images/map.png"), makeNewBitmapImage("/Images/collisionMap.png"))
		map.ui_updateMapPosition()
		gw.gameField.Children.Add(map.control)

	End Sub

	' start the game loop!
	Public Sub start()
		gameTimer = New Timers.Timer(1)
		AddHandler gameTimer.Elapsed, AddressOf gameLoop
		gameTimer.AutoReset = False
		gameTimer.Enabled = True
	End Sub

	' Reset for when you loose a life
	Private Sub reset()
		Canvas.SetZIndex(vicViper.control, 0)
		shallReset = False

		vicViper.resetState()

		map.position = 0
	End Sub
#End Region

#Region "Main Game Loop"
	' The game loop which runs once every ~15-16ms. Due to the nature of timers it is not always exact, however it is inconsequential and managable.
	Private Sub gameLoop(send As Object, e As Timers.ElapsedEventArgs)
		' delta time calculation (for accurate movement)
		Dim currentTime As DateTime
		currentTime = DateTime.Now
		deltaLoopTime = currentTime - previousTime
		previousTime = currentTime

		' Processing
		If checkCollisions() Then
			gw.Dispatcher.BeginInvoke(New InvokeDelegate(AddressOf destroyVicViper))
		ElseIf vicViper.isDying = False Then
			moveEntity(vicViper, deltaLoopTime)
		End If
		updateVicAnimationFrame()
		moveMap(deltaLoopTime)

		' UI Updating
		gw.Dispatcher.BeginInvoke(New InvokeDelegate(AddressOf updateUI))

		gameTimer.Enabled = True
	End Sub
#End Region

#Region "Processing Routines"

	' Check the collisions for the vicviper/entities
	Private Function checkCollisions()
		Dim hasDied As Boolean
		hasDied = False
		If Not vicViper.isDying Then
			checkBoundaries()
			hasDied = checkTerrain(vicViper)
		End If
		Return hasDied
	End Function

	Private Sub checkBoundaries()
		Dim previous As Directions
		previous = vicViper.freeDirections

		vicViper.freeDirections.up = vicViper.hitBox.Top > 4
		vicViper.freeDirections.left = vicViper.hitBox.Left > 4
		vicViper.freeDirections.down = vicViper.hitBox.Bottom < (gameHeight * scaleFactor) - 5
		vicViper.freeDirections.right = vicViper.hitBox.Right < (gameWidth * scaleFactor) - 5

		If previous <> vicViper.freeDirections Then
			updateVicVector()
		End If
	End Sub

	Private Function checkTerrain(ByVal entity As Entity)
		Dim collided As Boolean
		collided = False

		Dim topLeftCoord As Point
		topLeftCoord = New Point(CInt(Int((entity.hitBox.X - map.position) / (scaleFactor * 2))), CInt(Int((entity.hitBox.Y) / (scaleFactor * 2))))
		Dim bottomRightCoord As Point
		bottomRightCoord = New Point(CInt(Int((entity.hitBox.Right - map.position) / (scaleFactor * 2))), CInt(Int((entity.hitBox.Bottom) / (scaleFactor * 2))))

		Dim i As Integer = topLeftCoord.Y
		Do Until collided Or i > bottomRightCoord.Y
			Dim j As Integer = topLeftCoord.X
			Do Until collided Or j > bottomRightCoord.X
				If map.collisionMap(i, j) Then
					collided = True
				End If
				j = j + 1
			Loop
			i = i + 1
		Loop

		Return collided
	End Function

	' move the specified entity and update vicViper animation frame if applicable
	Private Sub moveEntity(ByVal entity As Entity, ByVal delta As TimeSpan)
		Dim distance = (delta.TotalMilliseconds / 1000) * entity.movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(entity.movementDirection * distance, entity.position)

		' fix for possible boundary errors
		If newPosition.X < 0 Then
			newPosition.X = 0
		ElseIf newPosition.X > (gameWidth * scaleFactor) - 1 Then
			newPosition.X = (gameWidth * scaleFactor) - 1
		End If
		If newPosition.Y < 0 Then
			newPosition.Y = 0
		ElseIf newPosition.Y > (gameHeight * scaleFactor) - 1 Then
			newPosition.Y = (gameHeight * scaleFactor) - 1
		End If

		entity.position = newPosition
	End Sub

	Private Sub moveMap(ByVal delta As TimeSpan)
		map.position = map.position - ((delta.TotalMilliseconds / 1000) * map.movementSpeed)

		If map.position <= (-map.length + (gameWidth * scaleFactor)) Then
			map.position = 0
		End If
	End Sub

	' update the animation frame of the vic viper
	Private Sub updateVicAnimationFrame()
		If Not vicViper.isDying Then
			' normal updating
			If vicAnimationTicking And (DateTime.Now - vicAnimationDelayStart).TotalMilliseconds >= vicAnimationDelay Then
				vicAnimationTicking = False
				If vicViper.movementDirection.Y > 0 Then
					vicViper.currentAnimationFrame = AnimationFrame.Down
				ElseIf vicViper.movementDirection.Y < 0 Then
					vicViper.currentAnimationFrame = AnimationFrame.Up
				End If
			End If
		Else
			' play death animation
			If (DateTime.Now - vicViper.deathAnimationDelayInterval).TotalMilliseconds >= vicViper.deathAnimationDelay Then
				vicViper.deathAnimationDelayInterval = DateTime.Now
				vicViper.deathFrameNum = vicViper.deathFrameNum + 1
			End If

			' check if should reset
			If (DateTime.Now - vicViper.deathAnimationDelayStart).TotalMilliseconds >= vicViper.vicResetDelay Then
				shallReset = True
			End If
		End If
	End Sub

	' Update the vicViper's movement vector and start animation frame delay
	Private Sub updateVicVector()
		Dim previous As Vector
		previous = vicViper.movementDirection

		If Not vicViper.isDying Then
			' calculate vector
			Dim newVector As Vector
			newVector = New Vector()
			If (vicViper.directionKeys.up And vicViper.directionKeys.down) Or Not (vicViper.directionKeys.up Or vicViper.directionKeys.down) Or (vicViper.directionKeys.up And Not vicViper.freeDirections.up) Or (vicViper.directionKeys.down And Not vicViper.freeDirections.down) Then
				newVector.Y = 0
			ElseIf vicViper.directionKeys.up Then
				newVector.Y = -1
			ElseIf vicViper.directionKeys.down Then
				newVector.Y = 1
			End If

			If (vicViper.directionKeys.left And vicViper.directionKeys.right) Or Not (vicViper.directionKeys.left Or vicViper.directionKeys.right) Or (vicViper.directionKeys.left And Not vicViper.freeDirections.left) Or (vicViper.directionKeys.right And Not vicViper.freeDirections.right) Then
				newVector.X = 0
			ElseIf vicViper.directionKeys.left Then
				newVector.X = -1
			ElseIf vicViper.directionKeys.right Then
				newVector.X = 1
			End If

			'If newVector.Length <> 0 Then
			'	newVector.Normalize()
			'End If

			vicViper.movementDirection = newVector
		End If

		' animation timer start
		If vicViper.movementDirection.Y = 0 Then
			vicAnimationTicking = False
			vicViper.currentAnimationFrame = AnimationFrame.Neutral
		ElseIf previous.Y <> vicViper.movementDirection.Y Then
			vicAnimationTicking = True
			vicAnimationDelayStart = DateTime.Now
		End If

	End Sub

	Private Sub destroyVicViper()
		vicViper.isDying = True
		lives = lives - 1
		Canvas.SetZIndex(vicViper.control, 50)
		vicViper.deathFrameNum = 0
		vicViper.deathAnimationDelayInterval = DateTime.Now
		vicViper.deathAnimationDelayStart = DateTime.Now
	End Sub
#End Region

#Region "UI Update Routines"
	Private Sub updateUI()
		If shallReset Then
			reset()
		Else
			vicViper.ui_updatePosition()
			vicViper.ui_updateFrame()
			map.ui_updateMapPosition()
			updateInterface()
		End If
	End Sub

	Private Sub updateInterface()
		gw.lblLives.Text = getLives()
	End Sub

#End Region

#Region "Other routines"
	' helper function to make a new sprite 
	Private Function makeNewSprite(ByVal source As String) As Image
		Dim newControl As New Image()
		Dim newBitmap As BitmapImage
		newBitmap = makeNewBitmapImage(source)
		newControl.Source = newBitmap
		newControl.Width = newBitmap.PixelWidth * scaleFactor
		newControl.Height = newBitmap.PixelHeight * scaleFactor
		Return newControl
	End Function

	' helper function to make new image for sprite
	Public Shared Function makeNewBitmapImage(ByVal source As String) As BitmapImage
		Dim newBitmap As New BitmapImage(New Uri("pack://application:,,," & source))
		Return newBitmap
	End Function
#End Region


End Class
