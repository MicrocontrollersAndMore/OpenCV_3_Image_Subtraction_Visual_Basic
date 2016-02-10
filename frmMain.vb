'Image_Subtraction_VB
'frmMain.vb
'
'Emgu CV 3.0.0
'
'form components:
'tableLayoutPanel
'btnOpenFile
'lblChosenFile
'ibOriginal
'txtInfo
'openFileDialog

Option Explicit On      'require explicit declaration of variables, this is NOT Python !!
Option Strict On        'restrict implicit data type conversions to only widening conversions

Imports Emgu.CV                 '
Imports Emgu.CV.CvEnum          'usual Emgu Cv imports
Imports Emgu.CV.Structure       '
Imports Emgu.CV.UI              '
Imports Emgu.CV.Util            '

'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
Public Class frmMain

    ' member variables ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Dim SCALAR_BLACK As New MCvScalar(0.0, 0.0, 0.0)
    Dim SCALAR_WHITE As New MCvScalar(255.0, 255.0, 255.0)
    Dim SCALAR_BLUE As New MCvScalar(255.0, 0.0, 0.0)
    Dim SCALAR_GREEN As New MCvScalar(0.0, 255.0, 0.0)
    Dim SCALAR_RED As New MCvScalar(0.0, 0.0, 255.0)

    Dim capVideo As Capture

    Dim blnFormClosing As Boolean = False

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub frmMain_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        blnFormClosing = True
        CvInvoke.DestroyAllWindows()
    End Sub
    
    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Private Sub btnOpenFile_Click(sender As Object, e As EventArgs) Handles btnOpenFile.Click

        Dim drChosenFile As DialogResult

        drChosenFile = openFileDialog.ShowDialog()                 'open file dialog

        If (drChosenFile <> DialogResult.OK Or openFileDialog.FileName = "") Then    'if user chose Cancel or filename is blank . . .
            lblChosenFile.Text = "file not chosen"              'show error message on label
            Return                                              'and exit function
        End If

        Try
            capVideo = New Capture(openFileDialog.FileName)        'attempt to open chosen video file
        Catch ex As Exception                                   'catch error if unsuccessful
                                                                'show error via message box
            MessageBox.Show("unable to read video file, error: " + ex.Message)
            Return
        End Try
        
        lblChosenFile.Text = openFileDialog.FileName

        If (capVideo Is Nothing) Then
            txtInfo.AppendText("unable to read video file")
        End If

        If (capVideo.GetCaptureProperty(CapProp.FrameCount) < 2) Then               'check and make sure the video has at least 2 frames
            txtInfo.AppendText("error: video file must have at least two frames")
        End If

        detectBlobsAndUpdateGUI()
        
    End Sub

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    Sub detectBlobsAndUpdateGUI()

        Dim imgFrame1 As Mat
        Dim imgFrame2 As Mat

        Dim blnFirstFrame As Boolean = True

        imgFrame1 = capVideo.QueryFrame()
        imgFrame2 = capVideo.QueryFrame()

        While (blnFormClosing = False)

            Dim blobs As New List(Of Blob)

            Dim imgFrame1Copy As Mat = imgFrame1.Clone()
            Dim imgFrame2Copy As Mat = imgFrame2.Clone()

            Dim imgDifference As New Mat(imgFrame1.Size, DepthType.Cv8U, 1)
            Dim imgThresh As New Mat(imgFrame1.Size, DepthType.Cv8U, 1)

            CvInvoke.CvtColor(imgFrame1Copy, imgFrame1Copy, ColorConversion.Bgr2Gray)
            CvInvoke.CvtColor(imgFrame2Copy, imgFrame2Copy, ColorConversion.Bgr2Gray)

            CvInvoke.GaussianBlur(imgFrame1Copy, imgFrame1Copy, New Size(5, 5), 0)
            CvInvoke.GaussianBlur(imgFrame2Copy, imgFrame2Copy, New Size(5, 5), 0)

            CvInvoke.AbsDiff(imgFrame1Copy, imgFrame2Copy, imgDifference)

            CvInvoke.Threshold(imgDifference, imgThresh, 30, 255.0, ThresholdType.Binary)

            CvInvoke.Imshow("imgThresh", imgThresh)

            Dim structuringElement3x3 As Mat = CvInvoke.GetStructuringElement(ElementShape.Rectangle, New Size(3, 3), New Point(-1, -1))
            Dim structuringElement5x5 As Mat = CvInvoke.GetStructuringElement(ElementShape.Rectangle, New Size(5, 5), New Point(-1, -1))
            Dim structuringElement7x7 As Mat = CvInvoke.GetStructuringElement(ElementShape.Rectangle, New Size(7, 7), New Point(-1, -1))
            Dim structuringElement9x9 As Mat = CvInvoke.GetStructuringElement(ElementShape.Rectangle, New Size(9, 9), New Point(-1, -1))

            CvInvoke.Dilate(imgThresh, imgThresh, structuringElement7x7, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0, 0, 0))
            CvInvoke.Erode(imgThresh, imgThresh, structuringElement3x3, New Point(-1, -1), 1, BorderType.Default, New MCvScalar(0, 0, 0))

            Dim imgThreshCopy As Mat = imgThresh.Clone()

            Dim contours As New VectorOfVectorOfPoint()

            CvInvoke.FindContours(imgThreshCopy, contours, Nothing, RetrType.External, ChainApproxMethod.ChainApproxSimple)

            For i As Integer = 0 To contours.Size() - 1

                Dim possibleBlob As New Blob(contours(i))

                If (possibleBlob.intRectArea > 100 And _
                    possibleBlob.dblAspectRatio >= 0.2 And _
                    possibleBlob.dblAspectRatio <= 1.2 And _
                    possibleBlob.boundingRect.Width > 15 And _
                    possibleBlob.boundingRect.Height > 20 And _
                    possibleBlob.dblDiagonalSize > 30.0) Then
                    blobs.Add(possibleBlob)
                End If
                
            Next

            Dim imgContours As New Mat(imgThresh.Size, DepthType.Cv8U, 3)

            contours = New VectorOfVectorOfPoint()              're-instiantate contours since contours.Clear() does not seem to work as expected

            For Each blob As Blob In blobs
                contours.Push(blob.contour)
            Next
            
            CvInvoke.DrawContours(imgContours, contours, -1, SCALAR_WHITE, -1)

            CvInvoke.Imshow("imgContours", imgContours)

            imgFrame2Copy = imgFrame2.Clone()           'get another copy of frame 2 since we changed the previous frame 2 copy in the processing above
            
            For Each blob As Blob In blobs                                              'for each blob
                CvInvoke.Rectangle(imgFrame2Copy, blob.boundingRect, SCALAR_RED, 2)             'draw a red box around the blob
                CvInvoke.Circle(imgFrame2Copy, blob.centerPosition, 3, SCALAR_GREEN, -1)        'draw a filled-in green circle at the center
            Next
            
            ibOriginal.Image = imgFrame2Copy

                    'now we prepare for the next iteration

            imgFrame1 = imgFrame2.Clone()                   'move frame 1 up to where frame 2 is

            If (capVideo.GetCaptureProperty(CapProp.PosFrames) + 1 < capVideo.GetCaptureProperty(CapProp.FrameCount)) Then      'if there is at least one more frame
                imgFrame2 = capVideo.QueryFrame()               'get the next frame
            Else                                                'else if there is not at least one more frame
                txtInfo.AppendText("end of video")              'show end of video message
                Exit While                                      'and jump out of while loop
            End If
            
            Application.DoEvents()

            blnFirstFrame = False

        End While
        
    End Sub
    
End Class













