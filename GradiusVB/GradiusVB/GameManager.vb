' Enum for different sounds
Public Enum SoundEffects
	Death
	GameOver
	Intro
	Volcano
	Shoot
	Laser
	Kill
	Collect
	PowerUp
	Start
End Enum

' Class to manage the playing of sound effects and background sounds.
Public Class SoundManager

	Private Delegate Sub soundEffectDelegate(ByVal effect As SoundEffects)

	Public backgroundPlayer As New MediaPlayer()
	Public bulletPlayer As New MediaPlayer()
	Public enemyPlayer As New MediaPlayer()

	Public currentMusic As SoundEffects

	Private gm As GameManager

	Public Sub New(ByVal gm As GameManager)
		Me.gm = gm
		currentMusic = SoundEffects.Intro
	End Sub

	Public Sub playSoundEffect(ByVal effect As SoundEffects)
		Select Case effect
			Case SoundEffects.Death, SoundEffects.GameOver, SoundEffects.Intro, SoundEffects.Volcano
				backgroundPlayer.Stop()
				playSound(backgroundPlayer, effect)
			Case SoundEffects.Shoot
				playSound(bulletPlayer, effect)
			Case SoundEffects.Kill, SoundEffects.Collect, SoundEffects.PowerUp, SoundEffects.Start
				playSound(enemyPlayer, effect)
		End Select
	End Sub

	Private Sub playSound(ByRef player As MediaPlayer, ByVal effect As SoundEffects)
		player = New MediaPlayer()
		Dim uri As New Uri("Sounds/death.wav", UriKind.Relative)
		Select Case effect
			Case SoundEffects.Death
				uri = New Uri("Sounds/death.wav", UriKind.Relative)
			Case SoundEffects.GameOver
				uri = New Uri("Sounds/gameOver.mp3", UriKind.Relative)
			Case SoundEffects.Intro
				uri = New Uri("Sounds/intro.mp3", UriKind.Relative)
			Case SoundEffects.Volcano
				uri = New Uri("Sounds/volcano.mp3", UriKind.Relative)
			Case SoundEffects.Shoot
				uri = New Uri("Sounds/shoot.wav", UriKind.Relative)
			Case SoundEffects.Kill
				uri = New Uri("Sounds/kill.wav", UriKind.Relative)
			Case SoundEffects.Collect
				uri = New Uri("Sounds/power_up.wav", UriKind.Relative)
			Case SoundEffects.PowerUp
				uri = New Uri("Sounds/select_power.wav", UriKind.Relative)
			Case SoundEffects.Start
				uri = New Uri("Sounds/start.wav", UriKind.Relative)
		End Select
		player.Open(uri)

		If gm.getSoundSetting = False Then
			player.Volume = 0
		End If

		player.Play()
	End Sub

End Class

' Structure to represent directions
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
	Three
	Four
	Five
End Enum

' Class for storing the representation of maps and terrain
Public Class Map

	Const mapMoveSpeed As Integer = 64

	Public control As Image

	Public collisionMap As Boolean(,)
	Public enemyMap As Entity(,)

	Public length As Integer
	Public position As Double
	Public movementSpeed As Double

	Public Sub New(ByVal control As Image, ByVal collisionImage As BitmapImage, ByVal enemyMapImage As BitmapImage, ByRef vicViper As VicViper)
		Me.control = control
		Me.length = control.Width
		Me.position = 0
		Me.movementSpeed = mapMoveSpeed

		Canvas.SetZIndex(Me.control, 2)

		' collisionmap generation
		Dim collisionPixelArray As Byte()
		collisionPixelArray = GameManager.getPixelData(collisionImage)

		collisionMap = New Boolean(collisionImage.PixelHeight - 1, collisionImage.PixelWidth - 1) {}

		Dim counter As Integer = 0
		For i = 0 To collisionImage.PixelHeight - 1
			For j = 0 To collisionImage.PixelWidth - 1
				If CInt(collisionPixelArray(counter)) > 0 Then
					collisionMap(i, j) = False
				Else
					collisionMap(i, j) = True
				End If
				counter = counter + 4
			Next j
		Next i

		' enemyMap generation
		enemyMap = GameManager.generateEnemyMap(GameManager.getPixelData(enemyMapImage), New Size(enemyMapImage.PixelWidth, enemyMapImage.PixelHeight), vicViper)

	End Sub

	Public Sub ui_updateMapPosition()
		Canvas.SetLeft(control, position)
	End Sub

End Class

#Region "Entities"
' Overall class for entities, including bullets, enemies, and the player
Public Class Entity

	Public movementDirection As Vector
	Public position As Point
	Public startingPosition As Point
	Public baseMovementSpeed As Double
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

	Public Const animationCycleTime As Integer = 200
	Public lastFrame As DateTime

	Public shallShoot As Boolean = False
	Public shootTime As Integer
	Public lastShootTime As DateTime
	Public shallChirp As Boolean = False

	Public shallDestroy As Boolean = False
	Public shallSpawn As Boolean = False

	Public value As Integer
	Public dropsPowerUp As Boolean = False
	Public formationNumber As Integer

	Public type As String
	Public control As Image

	Public Sub New(ByVal type As String, ByVal control As Image, ByVal position As Point, ByVal movementSpeed As Double)
		Me.type = type
		Me.control = control
		Me.position = position
		Me.startingPosition = position
		Me.baseMovementSpeed = movementSpeed
		Me.movementSpeed = movementSpeed
		Me.deathAnimationDelay = 64
		Me.deathFrameNum = -1
		Me.hitBox = New Rect(position.X, position.Y, control.Width, control.Height)

		Canvas.SetZIndex(control, -1)

		currentAnimationFrame = AnimationFrame.Neutral

		freeDirections.up = True
		freeDirections.down = True
		freeDirections.left = True
		freeDirections.right = True
	End Sub

	' position update
	Public Sub ui_updatePosition()
		Canvas.SetLeft(control, position.X)
		Canvas.SetTop(control, position.Y)
	End Sub

	' update what texture to show for this entity
	Public Sub ui_updateFrame()
		If Me.isDying Then
			If deathFrameNum >= deathAnimationFrames.Length Then
				control.Source = Nothing
			Else
				control.Source = deathAnimationFrames(deathFrameNum)
				control.Width = deathAnimationFrames(deathFrameNum).PixelWidth * GameManager.scaleFactor
				control.Height = deathAnimationFrames(deathFrameNum).PixelHeight * GameManager.scaleFactor
			End If
		Else
			control.Source = animationFrames(currentAnimationFrame)
		End If
	End Sub

	' apply the vector calculation to move the entity
	Public Overridable Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If hitBox.Right > GameManager.gameWidth * GameManager.scaleFactor - 1 Or position.X < 0 Or hitBox.Bottom > GameManager.gameHeight * GameManager.scaleFactor - 1 Or position.Y < 0 Then
			shallDestroy = True
		End If
	End Sub

	Public Overridable Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager, ByRef gm As GameManager)

	End Sub

	' function to reset the entity state
	Public Overridable Sub resetState()
		Me.position = startingPosition
		Me.shallDestroy = False
		Me.isDying = False
		Me.shallChirp = False
		Me.shallShoot = False
		Me.shallSpawn = False
		Me.deathFrameNum = -1
		currentAnimationFrame = AnimationFrame.Neutral
		ui_updatePosition()
		ui_updateFrame()
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

End Class

' class for enemy
Public Class Enemy_Fan
	Inherits Entity

	' behaviour variables
	Const turnPosition As Integer = 200
	Private aboveFan As Boolean = False
	Private hasTurned As Boolean = False
	Private hasStraightened As Boolean = False

	Sub New(ByVal position As Point)
		MyBase.New("fan", GameManager.makeNewSprite("/Images/fan_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/fan_1.png"), GameManager.makeNewBitmapImage("/Images/fan_2.png"), GameManager.makeNewBitmapImage("/Images/fan_3.png")}
		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/fan_death_1.png"), GameManager.makeNewBitmapImage("/Images/fan_death_2.png"), GameManager.makeNewBitmapImage("/Images/fan_death_3.png")}

		value = 100

		lastFrame = DateTime.Now
	End Sub

	' movement for the fan enemy
	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
		If position.X <= turnPosition And hasTurned = False Then
			hasTurned = True
			If position.Y > GameManager.gameHeight * GameManager.scaleFactor / 2 Then
				movementDirection = New Vector(0.5, -1)
			Else
				aboveFan = True
				movementDirection = New Vector(0.5, 1)
			End If

		End If

		If hasTurned And Not hasStraightened Then
			If (aboveFan And position.Y > vicViper.position.Y) Or (Not aboveFan And position.Y < vicViper.position.Y) Then
				hasStraightened = True
				movementDirection = New Vector(1.5, 0)
			End If
		End If


		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

	Public Overrides Sub resetState()
		MyBase.resetState()
		hasTurned = False
		hasStraightened = False
		movementDirection = New Vector(-1, 0)
	End Sub

End Class

' class for enemy
Public Class Enemy_Rugal
	Inherits Entity

	Sub New(ByVal position As Point)
		MyBase.New("rugal", GameManager.makeNewSprite("/Images/rugal_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/rugal_1.png"), GameManager.makeNewBitmapImage("/Images/rugal_2.png"), GameManager.makeNewBitmapImage("/Images/rugal_3.png")}
		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100
	End Sub

	' movement for rugal enemy
	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)

		If position.Y > vicViper.position.Y + 2 Then
			movementDirection = New Vector(-0.5, -0.15)
			currentAnimationFrame = AnimationFrame.Up
		ElseIf position.Y < vicViper.position.Y - 2 Then
			movementDirection = New Vector(-0.5, 0.15)
			currentAnimationFrame = AnimationFrame.Down
		Else
			movementDirection = New Vector(-1, 0)
			currentAnimationFrame = AnimationFrame.Neutral
		End If


		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

End Class

' class for enemy
Public Class Enemy_Garun
	Inherits Entity

	Private baseLinePos As Double

	Sub New(ByVal position As Point, ByVal isRed As Boolean)
		MyBase.New("garun", GameManager.makeNewSprite("/Images/garun_1.png"), position, 256)
		movementDirection = New Vector(-1, 0)

		baseLinePos = position.Y

		If isRed Then
			dropsPowerUp = True
			animationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/garun_1_red.png"), GameManager.makeNewBitmapImage("/Images/garun_2_red.png"), GameManager.makeNewBitmapImage("/Images/garun_3_red.png"), GameManager.makeNewBitmapImage("/Images/garun_4_red.png")}
		Else
			animationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/garun_1.png"), GameManager.makeNewBitmapImage("/Images/garun_2.png"), GameManager.makeNewBitmapImage("/Images/garun_3.png"), GameManager.makeNewBitmapImage("/Images/garun_4.png")}
		End If

		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100

		lastFrame = DateTime.Now
	End Sub

	' movement for garun enemy (sine wave)
	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)

		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition

		position.Y = baseLinePos + 35 * Math.Sin(0.0185 * position.X)

		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

End Class

' class for enemy
Public Class Enemy_Dee01
	Inherits Entity

	Private onRoof As Boolean
	Const baseShootTime As Integer = 3500
	Const shootTimeRange As Integer = 2000

	Sub New(ByVal position As Point, ByVal onRoof As Boolean)
		MyBase.New("dee01", GameManager.makeNewSprite("/Images/dee01_1.png"), position, 64)
		movementDirection = New Vector(-1, 0)

		shootTime = generateRandomShootTime()

		If onRoof Then
			Me.onRoof = True
			animationFrames = New BitmapImage(5) {GameManager.makeNewBitmapImage("/Images/dee01_1_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_2_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_3_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_4_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_5_r.png"), GameManager.makeNewBitmapImage("/Images/dee01_6_r.png")}
		Else
			Me.onRoof = False
			animationFrames = New BitmapImage(5) {GameManager.makeNewBitmapImage("/Images/dee01_6.png"), GameManager.makeNewBitmapImage("/Images/dee01_5.png"), GameManager.makeNewBitmapImage("/Images/dee01_4.png"), GameManager.makeNewBitmapImage("/Images/dee01_3.png"), GameManager.makeNewBitmapImage("/Images/dee01_2.png"), GameManager.makeNewBitmapImage("/Images/dee01_1.png")}
		End If

		deathAnimationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/death_1.png"), GameManager.makeNewBitmapImage("/Images/death_2.png"), GameManager.makeNewBitmapImage("/Images/death_3.png")}

		value = 100
	End Sub

	' movement (animation) for dee-01 enemy (turret)
	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)

		Dim angle As Double
		angle = getAngleToPlayer(vicViper)

		If angle <= Math.PI / 6 Then
			currentAnimationFrame = 0
		ElseIf angle <= Math.PI / 3 Then
			currentAnimationFrame = 1
		ElseIf angle <= Math.PI / 2 Then
			currentAnimationFrame = 2
		ElseIf angle <= 2 * Math.PI / 3 Then
			currentAnimationFrame = 3
		ElseIf angle <= 5 * Math.PI / 6 Then
			currentAnimationFrame = 4
		Else
			currentAnimationFrame = 5
		End If

		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

	' helper function to get the angle to the player from the turret (anti-clockwise from the negative x axis)
	Private Function getAngleToPlayer(ByRef vicViper As VicViper) As Double
		Dim angle As Double
		If onRoof Then
			angle = Math.Atan((vicViper.position.Y - position.Y) / (position.X - (vicViper.position.X + 32)))
		Else
			angle = Math.Atan((position.Y - vicViper.position.Y) / (position.X - (vicViper.position.X + 32)))
		End If

		If angle < 0 Then
			angle = Math.PI + angle
		End If
		Return angle
	End Function

	Public Shared Function generateRandomShootTime()
		Return baseShootTime + Int((shootTimeRange + 1) * Rnd() - shootTimeRange / 2)
	End Function

	' create the enemy bullet
	Public Overrides Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager, ByRef gm As GameManager)
		If Not isDying Then
			Dim bullet As Entity
			bullet = New Entity("bullet", GameManager.makeNewSprite("/Images/enemy_bullet.png"), Point.Add(position, New Size(control.Width / 2, control.Height / 2)), 128)
			Array.Resize(gm.currentBullets, gm.currentBullets.Length + 1)
			gm.currentBullets(gm.currentBullets.Length - 1) = bullet
			bullet.ui_updatePosition()
			bullet.movementDirection = New Vector((gm.vicViper.position.X + 32) - position.X, gm.vicViper.position.Y - position.Y)

			If bullet.movementDirection = New Vector(0, 0) Then
				bullet.movementDirection = New Vector(0, 1)
			Else
				bullet.movementDirection.Normalize()
			End If

			gw.gameField.Children.Add(bullet.control)
		End If
	End Sub

End Class

' class for enemy
Public Class PowerUp
	Inherits Entity

	Sub New(ByVal position As Point)
		MyBase.New("powerup", GameManager.makeNewSprite("/Images/powerup_1.png"), position, 64)
		movementDirection = New Vector(-1, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/powerup_1.png"), GameManager.makeNewBitmapImage("/Images/powerup_2.png"), GameManager.makeNewBitmapImage("/Images/powerup_3.png")}

		lastFrame = DateTime.Now
		value = 500
		shallSpawn = True
	End Sub

	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)

		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)
		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y

		If position.X > GameManager.gameWidth * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.X < -GameManager.enemyBufferZone Or position.Y > GameManager.gameHeight * GameManager.scaleFactor + GameManager.enemyBufferZone Or position.Y < -GameManager.enemyBufferZone Then
			shallDestroy = True
		End If
	End Sub

	Public Overrides Sub resetState()
		MyBase.resetState()
	End Sub

End Class

' enum for different ammo types
Public Enum AmmunitionType
	Normal
	Laser
	Rapid
End Enum

' The class for the player entity
Public Class VicViper
	Inherits Entity

	Public Const startingSpeed As Integer = 128
	Public Const startingPosX As Double = 50
	Public Const startingPosY As Double = 200
	Public Const vicResetDelay As Integer = 2000

	Private Const vicDeathDelayTime As Integer = 200

	Public currentBullets(-1) As Entity

	Public ammoType As AmmunitionType = AmmunitionType.Normal

	Public formationKills(-1) As Integer


	Public directionKeys As Directions

	Public Sub New(ByVal control As Image)
		MyBase.New("vic", control, New Point(startingPosX, startingPosY), startingSpeed)

		Canvas.SetZIndex(Me.control, 0)

		animationFrames = New BitmapImage(2) {GameManager.makeNewBitmapImage("/Images/vicViper.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Up.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Down.png")}
		deathAnimationFrames = New BitmapImage(3) {GameManager.makeNewBitmapImage("/Images/vicViper_Death_0.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_1.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_2.png"), GameManager.makeNewBitmapImage("/Images/vicViper_Death_3.png")}
		Me.deathAnimationDelay = vicDeathDelayTime
	End Sub

	Public Overrides Sub resetState()
		isDying = False
		position = New Point(startingPosX, startingPosY)
		movementDirection = New Vector(0, 0)
		movementSpeed = startingSpeed
		currentAnimationFrame = AnimationFrame.Neutral
		deathFrameNum = -1
		ammoType = AmmunitionType.Normal
	End Sub

	' handle movement
	Public Overrides Sub moveEntity(ByVal delta As TimeSpan, ByRef vicViper As VicViper)
		Dim distance = (delta.TotalMilliseconds / 1000) * movementSpeed
		Dim newPosition As Point
		newPosition = Vector.Add(movementDirection * distance, position)

		' fix for possible boundary errors
		If newPosition.X < 0 Then
			newPosition.X = 0
		ElseIf newPosition.X > (GameManager.gameWidth * GameManager.scaleFactor) - 1 Then
			newPosition.X = (GameManager.gameWidth * GameManager.scaleFactor) - 1
		End If
		If newPosition.Y < 0 Then
			newPosition.Y = 0
		ElseIf newPosition.Y > (GameManager.gameHeight * GameManager.scaleFactor) - 1 Then
			newPosition.Y = (GameManager.gameHeight * GameManager.scaleFactor) - 1
		End If

		position = newPosition
		hitBox.X = position.X
		hitBox.Y = position.Y
	End Sub

	' update the bullets
	Public Sub ui_updateBullets(ByRef gw As GameWindow)
		Dim toDestroy(-1) As Integer

		For i = 0 To currentBullets.Length - 1
			If currentBullets(i).shallDestroy Then
				Array.Resize(toDestroy, toDestroy.Length + 1)
				toDestroy(toDestroy.Length - 1) = i
				gw.gameField.Children.Remove(currentBullets(i).control)
			End If
			currentBullets(i).ui_updatePosition()
		Next i

		GameManager.removeFromArray(currentBullets, toDestroy)

	End Sub

	' generate a bullet
	Public Overrides Sub generateBullet(ByRef gw As GameWindow, ByRef sm As SoundManager, ByRef gm As GameManager)
		If (currentBullets.Length < 2 Or ammoType = AmmunitionType.Rapid) And Not isDying Then
			sm.playSoundEffect(SoundEffects.Shoot)
			Dim bullet As Entity

			If ammoType = AmmunitionType.Laser Then
				bullet = New Entity("bullet", GameManager.makeNewSprite("/Images/laser.png"), Point.Add(position, New Size(control.Width / 2, control.Height / 2)), 1200)
			Else
				bullet = New Entity("bullet", GameManager.makeNewSprite("/Images/vic_Bullet.png"), Point.Add(position, New Size(control.Width / 2, control.Height / 2)), 750)
			End If

			Array.Resize(currentBullets, currentBullets.Length + 1)
			currentBullets(currentBullets.Length - 1) = bullet
			bullet.ui_updatePosition()
			bullet.movementDirection = New Vector(1, 0)
			gw.gameField.Children.Add(bullet.control)
		End If
	End Sub

End Class

#End Region

' main class to handle game processing and logic flow
Public Class GameManager

	Public gw As GameWindow

	Private name As String
	Private soundSetting As Boolean
	Private highScore As Integer

#Region "GameSpecific"

	Public Shared scaleFactor As Integer = 2
	Public Shared gameWidth As Integer = 256
	Public Shared gameHeight As Integer = 208
	Public Shared enemyBufferZone As Integer = 64

	Const vicAnimationDelay As Integer = 100
	Const gameOverDelay As Integer = 2500

	Public vicViper As VicViper

	Public map As Map
	Private previousEnemyCheckPosition As Double

	Public sm As SoundManager

	Private lives As Integer
	Private score As Integer

	Private gameTimer As Timers.Timer
	Private previousTime As DateTime
	Private deltaLoopTime As TimeSpan

	Private powerUpImages As Image()
	Private powerUpSelected As Integer

	Private vicAnimationTicking As Boolean
	Private vicAnimationDelayStart As DateTime

	Private shallReset As Boolean = False
	Private shallEndGame As Boolean = False
	Private gameOver As Boolean = False
	Private gameOverDelayStart As DateTime

	Private currentEnemies(-1) As Entity
	Public currentBullets(-1) As Entity

	Private movementSpeedMultiplier As Double = 1


#End Region

#Region "Delegates"
	Private Delegate Sub InvokeDelegate()
	Private Delegate Sub PowerUpDelegate(ByVal position As Point)
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

	Public Function getScore() As Integer
		Return score
	End Function

	Public Function getHighScore() As Integer
		Return highScore
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

	' Signal to shoot!
	Public Sub prepareToShoot()
		vicViper.shallShoot = True
	End Sub

	' apply a power-up
	Public Sub selectPowerUp()
		If Not vicViper.isDying Then
			Select Case powerUpSelected
				Case 0
					vicViper.movementSpeed = vicViper.movementSpeed * 1.2
				Case 1
					vicViper.ammoType = AmmunitionType.Laser
				Case 2
					vicViper.ammoType = AmmunitionType.Rapid
			End Select
			sm.playSoundEffect(SoundEffects.PowerUp)
			powerUpSelected = -1
		End If
	End Sub

#End Region

#Region "Initialisation/Reset Routines"

	' setup before game window created
	Private Sub preSetup()
		lives = 3
		score = 0
		highScore = HighScoreManager.getHighScore()
		sm = New SoundManager(Me)
	End Sub

	' setup when game window created
	Public Sub setup()
		powerUpImages = New Image(2) {gw.pm_Speed, gw.pm_Laser, gw.pm_Shield}
		powerUpSelected = -1

		previousTime = DateTime.Now

		vicViper = New VicViper(makeNewSprite("/Images/vicViper.png"))
		vicViper.ui_updatePosition()
		gw.gameField.Children.Add(vicViper.control)

		map = New Map(makeNewSprite("/Images/map.png"), makeNewBitmapImage("/Images/collisionMap.png"), makeNewBitmapImage("/Images/enemy_map.png"), vicViper)
		map.ui_updateMapPosition()
		gw.gameField.Children.Add(map.control)

		previousEnemyCheckPosition = -map.position + (gameWidth * scaleFactor) + enemyBufferZone
	End Sub

	' start the game loop!
	Public Sub start()
		sm.playSoundEffect(SoundEffects.Intro)
		gameTimer = New Timers.Timer(1)
		AddHandler gameTimer.Elapsed, AddressOf gameLoop
		gameTimer.AutoReset = False
		gameTimer.Enabled = True

	End Sub

	' Reset for when you loose a life
	Private Sub reset()

		For i = 0 To currentEnemies.Length - 1
			currentEnemies(i).resetState()
			gw.gameField.Children.Remove(currentEnemies(i).control)
		Next i

		ReDim currentEnemies(-1)

		For j = 0 To currentBullets.Length - 1
			gw.gameField.Children.Remove(currentBullets(j).control)
		Next j
		ReDim currentBullets(-1)

		For i = 0 To vicViper.formationKills.Length - 1
			vicViper.formationKills(i) = 0
		Next i

		If powerUpSelected >= 0 Then
			powerUpSelected = 0
		Else
			powerUpSelected = -1
		End If

		Canvas.SetZIndex(vicViper.control, 0)
		shallReset = False

		vicViper.resetState()

		map.position = 0
		sm.playSoundEffect(SoundEffects.Intro)
		sm.currentMusic = SoundEffects.Intro
	End Sub

	' close the window, display highscores.
	Private Sub endGame()
		sm.playSoundEffect(SoundEffects.GameOver)
		Dim sw As ScoreWindow
		sw = New ScoreWindow(Me)
		gw.Close()
		sw.Show()
	End Sub

#End Region

#Region "Main Game Loop"
	' The game loop which runs once every ~15-16ms. This is the main line of the program. Due to the nature of timers it is not always exact, however it is inconsequential and managable.
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
		moveEntities(deltaLoopTime)
		updateTimedRoutines()
		moveMap(deltaLoopTime)

		' UI Updating
		gw.Dispatcher.BeginInvoke(New InvokeDelegate(AddressOf updateUI))

		If shallEndGame = False Then
			gameTimer.Enabled = True
		End If
	End Sub
#End Region

#Region "Processing Routines"

	' Check the collisions for the vicviper/entities
	Private Function checkCollisions()

		Dim hasDied As Boolean
		hasDied = False

		' enemy bullets checking
		For j = 0 To currentBullets.Length - 1
			If checkTerrain(currentBullets(j)) Then
				currentBullets(j).shallDestroy = True
			Else
				Dim hit As Entity = checkHitsForBullet(currentBullets(j), False)
				If hit IsNot Nothing Then
					currentBullets(j).shallDestroy = True
					hasDied = True
				End If
			End If
		Next j

		' vic viper bullets checking
		For i = 0 To vicViper.currentBullets.Length - 1
			If checkTerrain(vicViper.currentBullets(i)) Then
				vicViper.currentBullets(i).shallDestroy = True
			Else
				Dim enemy As Entity = checkHitsForBullet(vicViper.currentBullets(i), True)
				If enemy IsNot Nothing Then
					score = score + enemy.value
					If vicViper.ammoType <> AmmunitionType.Laser Then
						vicViper.currentBullets(i).shallDestroy = True
					End If
					enemy.isDying = True
					enemy.shallChirp = True

					If enemy.formationNumber <> 0 Then
						vicViper.formationKills(enemy.formationNumber) = vicViper.formationKills(enemy.formationNumber) + 1

						For j = 0 To vicViper.formationKills.Length - 1
							If vicViper.formationKills(j) = 4 Then
								gw.Dispatcher.Invoke(New PowerUpDelegate(AddressOf preparePowerUp), enemy.position)
								vicViper.formationKills(j) = 0
							End If
						Next j

					End If

					If enemy.dropsPowerUp Then
						gw.Dispatcher.Invoke(New PowerUpDelegate(AddressOf preparePowerUp), enemy.position)
					End If

				End If
			End If
		Next i


		' vicViper checking
		If Not vicViper.isDying And Not hasDied Then
			checkBoundaries()
			checkPowerUps()
			hasDied = checkTerrain(vicViper) Or checkEnemyCollision()
		End If
		Return hasDied
	End Function

	' check the edges of the screen to prevent movement outside the game screen
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

	' check for collisions with powerups.
	Private Sub checkPowerUps()
		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).type = "powerup" AndAlso vicViper.hitBox.IntersectsWith(currentEnemies(i).hitBox) Then
				currentEnemies(i).shallDestroy = True
				currentEnemies(i).shallChirp = True
				score = score + currentEnemies(i).value
				powerUpSelected = (powerUpSelected + 1) Mod powerUpImages.Length
			End If
		Next i
	End Sub

	' check collisions with the terrain
	Private Function checkTerrain(ByVal entity As Entity)
		Dim collided As Boolean
		collided = False
		If Not entity.shallDestroy Then
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
		End If
		Return collided
	End Function

	' check any hits for a bullet
	Private Function checkHitsForBullet(ByRef bullet As Entity, ByVal playerBullet As Boolean) As Entity
		Dim hitEnemy As Entity
		hitEnemy = Nothing

		If playerBullet Then
			For i = 0 To currentEnemies.Length - 1
				If bullet.hitBox.IntersectsWith(currentEnemies(i).hitBox) And Not currentEnemies(i).isDying And Not currentEnemies(i).type = "powerup" Then
					hitEnemy = currentEnemies(i)
				End If
			Next i
		Else
			If bullet.hitBox.IntersectsWith(vicViper.hitBox) And Not vicViper.isDying Then
				hitEnemy = vicViper
			End If
		End If

		Return hitEnemy
	End Function

	' check for a player collision with the enemy
	Private Function checkEnemyCollision() As Boolean
		Dim hit As Boolean = False
		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).hitBox.IntersectsWith(vicViper.hitBox) And Not currentEnemies(i).isDying And Not currentEnemies(i).type = "powerup" Then
				hit = True
			End If
		Next i
		Return hit
	End Function

	' move the specified entity and update vicViper animation frame if applicable
	Private Sub moveEntity(ByVal entity As Entity, ByVal delta As TimeSpan)
		entity.moveEntity(delta, vicViper)
	End Sub

	' move all entities
	Private Sub moveEntities(ByVal delta As TimeSpan)

		' move enemies
		For i = 0 To currentEnemies.Length - 1
			If Not currentEnemies(i).isDying Then
				currentEnemies(i).moveEntity(delta, vicViper)
			End If
		Next i

		' move bullets
		For i = 0 To currentBullets.Length - 1
			currentBullets(i).moveEntity(delta, vicViper)
		Next i

		' move player bullets
		For i = 0 To vicViper.currentBullets.Length - 1
			vicViper.currentBullets(i).moveEntity(delta, vicViper)
		Next i
	End Sub

	' map scrolling
	Private Sub moveMap(ByVal delta As TimeSpan)
		map.position = map.position - ((delta.TotalMilliseconds / 1000) * map.movementSpeed)

		If map.position <= (-map.length + (gameWidth * scaleFactor)) Then
			map.position = 0
		End If

		' enemy spawning
		Dim currentEnemyCheckPosition As Double
		currentEnemyCheckPosition = -map.position + (gameWidth * scaleFactor) + enemyBufferZone

		Dim startPos As Integer
		startPos = CInt(Int(previousEnemyCheckPosition / (2 * scaleFactor))) Mod map.enemyMap.GetLength(1)
		Dim endPos As Integer
		endPos = CInt(Int(currentEnemyCheckPosition / (2 * scaleFactor))) Mod map.enemyMap.GetLength(1)

		If endPos - startPos > 0 Then
			' check columns for startPos + 1 -> endPos inclusive
			For i = startPos + 1 To endPos
				For j = 0 To map.enemyMap.GetLength(0) - 1
					If map.enemyMap(j, i) IsNot Nothing Then
						prepareEnemy(map.enemyMap(j, i))
					End If
				Next j
			Next i

		End If

		previousEnemyCheckPosition = currentEnemyCheckPosition

	End Sub

	' update and check events which happen on a timed basis
	Private Sub updateTimedRoutines()
		If Not vicViper.isDying Then
			updateVicAnimationFrame()
		Else
			' play death animation
			If (DateTime.Now - vicViper.deathAnimationDelayInterval).TotalMilliseconds >= vicViper.deathAnimationDelay Then
				vicViper.deathAnimationDelayInterval = DateTime.Now
				vicViper.deathFrameNum = vicViper.deathFrameNum + 1
			End If

			' check if should reset
			If (DateTime.Now - vicViper.deathAnimationDelayStart).TotalMilliseconds >= vicViper.vicResetDelay And Not gameOver Then
				shallReset = True
			End If

			' check if should end game
			If gameOver AndAlso (DateTime.Now - gameOverDelayStart).TotalMilliseconds >= gameOverDelay Then
				shallEndGame = True
			End If
		End If

		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).type = "fan" Or currentEnemies(i).type = "garun" Or currentEnemies(i).type = "powerup" Then

				If Not currentEnemies(i).isDying Then
					' regular animation
					If (DateTime.Now - currentEnemies(i).lastFrame).TotalMilliseconds >= Entity.animationCycleTime Then
						currentEnemies(i).currentAnimationFrame = (CInt(currentEnemies(i).currentAnimationFrame) + 1) Mod currentEnemies(i).animationFrames.Length
						currentEnemies(i).lastFrame = DateTime.Now
					End If
				End If
			ElseIf currentEnemies(i).type = "dee01" Then
				' enemyShooting
				If (DateTime.Now - currentEnemies(i).lastShootTime).TotalMilliseconds >= currentEnemies(i).shootTime Then
					currentEnemies(i).shallShoot = True
					currentEnemies(i).shootTime = Enemy_Dee01.generateRandomShootTime()
					currentEnemies(i).lastShootTime = DateTime.Now
				End If
			End If

			If currentEnemies(i).isDying Then
				If (DateTime.Now - currentEnemies(i).deathAnimationDelayInterval).TotalMilliseconds >= currentEnemies(i).deathAnimationDelay Then
					If currentEnemies(i).deathFrameNum >= currentEnemies(i).deathAnimationFrames.Length Then
						currentEnemies(i).shallDestroy = True
					End If
					currentEnemies(i).deathAnimationDelayInterval = DateTime.Now
					currentEnemies(i).deathFrameNum = currentEnemies(i).deathFrameNum + 1
				End If
			End If
		Next i

	End Sub

	' update the animation frame of the vic viper
	Private Sub updateVicAnimationFrame()
		If vicAnimationTicking And (DateTime.Now - vicAnimationDelayStart).TotalMilliseconds >= vicAnimationDelay Then
			vicAnimationTicking = False
			If vicViper.movementDirection.Y > 0 Then
				vicViper.currentAnimationFrame = AnimationFrame.Down
			ElseIf vicViper.movementDirection.Y < 0 Then
				vicViper.currentAnimationFrame = AnimationFrame.Up
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

	' destroy the player, loose a life
	Private Sub destroyVicViper()
		vicViper.isDying = True
		lives = lives - 1

		If lives < 0 Then
			gameOver = True
			gameOverDelayStart = DateTime.Now
		End If

		Canvas.SetZIndex(vicViper.control, 50)
		vicViper.deathFrameNum = 0
		vicViper.deathAnimationDelayInterval = DateTime.Now
		vicViper.deathAnimationDelayStart = DateTime.Now

		sm.playSoundEffect(SoundEffects.Death)

	End Sub
#End Region

#Region "UI Update Routines"
	' updates all the UI elements
	Private Sub updateUI()

		' enemies, enemy bullets
		Dim enemiesToDestroy(-1) As Integer
		For i = 0 To currentEnemies.Length - 1
			If currentEnemies(i).shallSpawn Then
				gw.gameField.Children.Add(currentEnemies(i).control)
				currentEnemies(i).shallSpawn = False
				currentEnemies(i).lastShootTime = DateTime.Now
				If currentEnemies(i).type <> "dee01" And currentEnemies(i).type <> "powerup" Then
					currentEnemies(i).movementSpeed = currentEnemies(i).baseMovementSpeed * movementSpeedMultiplier
				End If
			ElseIf currentEnemies(i).shallDestroy Then
				If currentEnemies(i).shallChirp Then
					sm.playSoundEffect(SoundEffects.Collect)
				End If
				currentEnemies(i).resetState()
				Array.Resize(enemiesToDestroy, enemiesToDestroy.Length + 1)
				enemiesToDestroy(enemiesToDestroy.Length - 1) = i
				gw.gameField.Children.Remove(currentEnemies(i).control)
			ElseIf currentEnemies(i).shallChirp Then
				sm.playSoundEffect(SoundEffects.Kill)
				currentEnemies(i).shallChirp = False
			ElseIf currentEnemies(i).shallShoot Then
				currentEnemies(i).generateBullet(gw, sm, Me)
				currentEnemies(i).shallShoot = False
			End If
			currentEnemies(i).ui_updatePosition()
			currentEnemies(i).ui_updateFrame()
		Next i
		updateBullets()

		removeFromArray(currentEnemies, enemiesToDestroy)

		' vic Viper
		If vicViper.shallShoot Then
			vicViper.generateBullet(gw, sm, Me)
			vicViper.shallShoot = False
		End If

		If shallReset Then
			reset()
		ElseIf shallEndGame Then
			endGame()
		Else
			vicViper.ui_updatePosition()
			vicViper.ui_updateFrame()
			vicViper.ui_updateBullets(gw)
			map.ui_updateMapPosition()
			updateBGM()
			updateInterface()
		End If
	End Sub

	' update the positions of bullets
	Private Sub updateBullets()
		Dim toDestroy(-1) As Integer

		For i = 0 To currentBullets.Length - 1
			If currentBullets(i).shallDestroy Then
				Array.Resize(toDestroy, toDestroy.Length + 1)
				toDestroy(toDestroy.Length - 1) = i
				gw.gameField.Children.Remove(currentBullets(i).control)
			End If
			currentBullets(i).ui_updatePosition()
		Next i

		GameManager.removeFromArray(currentBullets, toDestroy)
	End Sub

	' updates text elements
	Private Sub updateInterface()
		If lives >= 0 Then
			gw.lblLives.Text = getLives()
		Else
			gw.lblLives.Visibility = Visibility.Hidden
		End If
		gw.lblScore.Text = getScore().ToString("D7")

		gw.pm_Speed.Source = makeNewBitmapImage("/Images/PM_Speed.png")
		gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Laser.png")
		gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Shield.png")
		Select Case powerUpSelected
			Case -1
				Select Case vicViper.ammoType
					Case AmmunitionType.Laser
						gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Blank.png")
					Case AmmunitionType.Rapid
						gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Blank.png")
				End Select
			Case 0
				gw.pm_Speed.Source = makeNewBitmapImage("/Images/PM_Speed_O.png")
				Select Case vicViper.ammoType
					Case AmmunitionType.Laser
						gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Blank.png")
					Case AmmunitionType.Rapid
						gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Blank.png")
				End Select
			Case 1
				gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Laser_O.png")
				Select Case vicViper.ammoType
					Case AmmunitionType.Laser
						gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Blank_O.png")
					Case AmmunitionType.Rapid
						gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Blank.png")
				End Select
			Case 2
				gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Shield_O.png")
				Select Case vicViper.ammoType
					Case AmmunitionType.Laser
						gw.pm_Laser.Source = makeNewBitmapImage("/Images/PM_Blank.png")
					Case AmmunitionType.Rapid
						gw.pm_Shield.Source = makeNewBitmapImage("/Images/PM_Blank_O.png")
				End Select
		End Select
	End Sub

	' updates which BGM to use. also multiplies enemy movement speed!
	Private Sub updateBGM()
		If sm.currentMusic = SoundEffects.Intro AndAlso Not vicViper.isDying AndAlso (map.position < -810 * scaleFactor And map.position > -820 * scaleFactor) Then
			sm.currentMusic = SoundEffects.Volcano
			sm.playSoundEffect(SoundEffects.Volcano)
		ElseIf sm.currentMusic = SoundEffects.Volcano AndAlso Not vicViper.isDying AndAlso map.position < -3320 * scaleFactor Then
			sm.currentMusic = SoundEffects.Intro
			sm.playSoundEffect(SoundEffects.Intro)
			movementSpeedMultiplier = movementSpeedMultiplier + 0.2
		End If
	End Sub

#End Region

#Region "Other routines"

	' prepare an enemy for spawning
	Private Sub prepareEnemy(ByVal enemy As Entity)
		Array.Resize(currentEnemies, currentEnemies.Length + 1)
		enemy.shallSpawn = True
		currentEnemies(currentEnemies.Length - 1) = enemy
	End Sub

	' prepare a powerup for spawning
	Private Sub preparePowerUp(ByVal position As Point)
		Dim powerUp As PowerUp
		powerUp = New PowerUp(position)
		Array.Resize(currentEnemies, currentEnemies.Length + 1)
		currentEnemies(currentEnemies.Length - 1) = powerUp
	End Sub

	' remove indexes from an array
	Public Shared Sub removeFromArray(ByRef array As Array, ByVal indexesToRemove As Integer())
		System.Array.Reverse(indexesToRemove)
		For i = 0 To indexesToRemove.Length - 1
			Dim newArray(array.Length - 2) As Entity
			If newArray.Length <> 0 Then
				array.Copy(array, 0, newArray, 0, indexesToRemove(i))
				array.Copy(array, indexesToRemove(i) + 1, newArray, indexesToRemove(i), array.Length - 1 - indexesToRemove(i))
			End If
			array = newArray
		Next i
	End Sub

	' helper function to make a new sprite 
	Public Shared Function makeNewSprite(ByVal source As String) As Image
		Dim newControl As New Image()
		Dim newBitmap As BitmapImage
		newBitmap = makeNewBitmapImage(source)
		newControl.Source = newBitmap
		newControl.Width = newBitmap.PixelWidth * GameManager.scaleFactor
		newControl.Height = newBitmap.PixelHeight * GameManager.scaleFactor
		Return newControl
	End Function

	' helper function to make new image for sprite
	Public Shared Function makeNewBitmapImage(ByVal source As String) As BitmapImage
		Dim newBitmap As New BitmapImage(New Uri("pack://application:,,," & source))
		Return newBitmap
	End Function

	' helper to generate byte array from bitmap
	Public Shared Function getPixelData(ByVal collisionImage As BitmapImage) As Byte()
		Dim stride As Integer = collisionImage.PixelWidth * collisionImage.Format.BitsPerPixel / 8
		Dim pixelArray((collisionImage.PixelHeight * stride) - 1) As Byte
		collisionImage.CopyPixels(pixelArray, stride, 0)
		Return pixelArray
	End Function

	'' helper to generate map of enemies
	Public Shared Function generateEnemyMap(ByVal pixelArray As Byte(), ByVal dimensions As Size, ByRef vicViper As VicViper) As Entity(,)
		Dim enemyMap(dimensions.Height - 1, dimensions.Width - 1) As Entity

		Dim formationCounter As Integer = 0
		Dim formationNumber As Integer = 1

		Dim row As Integer = 0
		Dim column As Integer = 0

		For i = 0 To pixelArray.Length - 1 Step 4
			Dim colour As Color
			colour = Color.FromRgb(pixelArray(i + 2), pixelArray(i + 1), pixelArray(i))
			Dim toAdd As Entity
			toAdd = Nothing

			Select Case colour.ToString()
				Case "#FFFF0000"
					toAdd = New Enemy_Fan(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor))
					toAdd.formationNumber = formationNumber
					formationCounter = formationCounter + 1
				Case ("#FF00FF00")
					toAdd = New Enemy_Rugal(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor))
				Case "#FF0000FF"
					toAdd = New Enemy_Garun(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), False)
				Case "#FF00F0FF"
					toAdd = New Enemy_Garun(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), True)
				Case "#FFFFFF00"
					toAdd = New Enemy_Dee01(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), False)
				Case "#FFFF00FF"
					toAdd = New Enemy_Dee01(New Point((gameWidth * scaleFactor) + enemyBufferZone, row * 2 * scaleFactor), True)
			End Select

			enemyMap(row, column) = toAdd

			column = column + 1
			If column = dimensions.Width Then
				column = 0
				row = row + 1
			End If

			If formationCounter = 4 Then
				formationCounter = 0
				formationNumber = formationNumber + 1
			End If

		Next i

		ReDim vicViper.formationKills(formationNumber)

		For i = 0 To vicViper.formationKills.Length - 1
			vicViper.formationKills(i) = 0
		Next i

		Return enemyMap
	End Function

#End Region

End Class
