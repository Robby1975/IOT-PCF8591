' The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

''' <summary>
''' An empty page that can be used on its own or navigated to within a Frame.
''' </summary>
Imports System
Imports System.Threading
Imports Windows.UI.Xaml.Controls
Imports Windows.Devices.Enumeration
Imports Windows.Devices.I2C
Imports Windows.Devices.Gpio


Structure Voltage
    Public valueC0 As Double
    Public valueC1 As Double
    Public valueC2 As Double
    Public valueC3 As Double
End Structure






Public NotInheritable Class MainPage
    Inherits Page
    Private Const PCF8591_I2C_ADDR As Byte = &H90           ' 7-bit I2C address Of thePFC8591 */
    Private Const DAC_Enable As Byte = &H40               ' Address Of the register to enable DAC */
    Private Const ADC_Channel3 As Byte = &H43             ' Address Of the third AD channel register */

    Private PCF8591 As Windows.Devices.I2C.I2cDevice
    Private periodicTimer As Timer




    Private Async Sub Init_I2C_PCF8591()
        Try
            Dim settings = New I2cConnectionSettings(PCF8591_I2C_ADDR >> 1)
            settings.BusSpeed = I2cBusSpeed.FastMode


            Dim aqs As String = I2cDevice.GetDeviceSelector()
            ' Get a selector string that will return all I2C controllers on the system 
            Dim dis = Await DeviceInformation.FindAllAsync(aqs)
            ' Find the I2C bus controller devices with our selector string             
            PCF8591 = Await I2cDevice.FromIdAsync(dis(0).Id, settings)
            ' Create an I2cDevice with our selected bus controller and I2C settings    
            If PCF8591 Is Nothing Then
                Text_Status.Text = String.Format("Slave address {0} on I2C Controller {1} is currently in use by " + "another application. Please ensure that no other applications are using I2C.", settings.SlaveAddress, dis(0).Id)
                Return
            End If
        Catch ex As Exception
            Text_Status.Text = "I2C Initialization failed. Exception: " + ex.Message
            Return
        End Try

        ' Now that everything is initialized, create a timer so we read data every 10mS 
        Dim TimerDelegate As New System.Threading.TimerCallback(AddressOf TimerCallback)
        periodicTimer = New Timer(TimerDelegate, Nothing, 0, 10)
    End Sub






    Private Function Read_I2C_PCF8591() As Voltage
        Const ADC_RES As Integer = 256                               ' The PF8591 has 8 bit resolution giving 256 unique values                     */
        Dim RegAddrBuf As Byte() = New Byte() {ADC_Channel3}              ' /* Register address (Channel3) we want To read from                                         */
        Dim ReadBuf As Byte() = New Byte(2) {}                          ' 

        'Read from the ADC 
        PCF8591.WriteRead(RegAddrBuf, ReadBuf)
        ' Convert raw values to volt
        Dim volt = ((CShort(ReadBuf(0))) / ADC_RES) * 3.29
        Dim v As Voltage
        v.valueC3 = volt
        Return v
    End Function



    Private Sub MainPage_Unloaded(sender As Object, args As Object)
        ' Cleanup 
        PCF8591.Dispose()

    End Sub



    Private Sub TimerCallback(state As Object)

        Dim c3Text As String

        Dim statusText As String

        'Read And format accelerometer data */
        Try
            Dim ADC As Voltage = Read_I2C_PCF8591()
            c3Text = String.Format("Channel 3: {0:F3}V", ADC.valueC3)
            statusText = "Status: Running"
        Catch ex As Exception
            c3Text = "PCF8591: Error"
            statusText = "Failed to read from PCF8591: " & ex.Message
        End Try

        '/* UI updates must be invoked on the UI thread */
        Dim Task = Me.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, Sub()
                                                                                             Text_C3.Text = c3Text
                                                                                             Text_Status.Text = statusText
                                                                                         End Sub)


    End Sub








    Private Sub MainPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Me.InitializeComponent()
        Try
            PCF8591.Dispose()

        Catch ex As Exception

        End Try

        'Register for the unloaded event so we can clean up upon exit
        AddHandler Unloaded, AddressOf MainPage_Unloaded

        ' Initialize the I2C bus, ADC/DCA, And timer 
        Init_I2C_PCF8591()
    End Sub

    Private Sub button_Click(sender As Object, e As RoutedEventArgs) Handles button.Click
        Me.InitializeComponent()
        Try
            PCF8591.Dispose()
        Catch ex As Exception
        End Try

        'Register for the unloaded event so we can clean up upon exit
        AddHandler Unloaded, AddressOf MainPage_Unloaded
        ' Initialize the I2C bus, ADC, And timer 
        Init_I2C_PCF8591()
    End Sub
End Class


