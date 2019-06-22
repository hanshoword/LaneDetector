using System;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using System.Collections.Generic;

namespace TrafficManagement
{
    class VehicleDetection : IDisposable
    {
        IplImage haarvehicle;
        IplImage gray;

        public IplImage VehicleDetect(IplImage src)
        {
            haarvehicle = new IplImage(src.Size, BitDepth.U8, 3);
            Cv.Copy(src, haarvehicle);

            gray = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, gray, ColorConversion.BgrToGray);

            Cv.EqualizeHist(gray, gray);

            double scaleFactor = 1.139;
            int minNeighbors = 1;

            CvHaarClassifierCascade cascade = CvHaarClassifierCascade.FromFile("../../../cars.xml");
            CvMemStorage Storage = new CvMemStorage();

            CvSeq<CvAvgComp> vehicles = Cv.HaarDetectObjects(gray, cascade, Storage, scaleFactor, minNeighbors, HaarDetectionType.ScaleImage, new CvSize(90, 90), new CvSize(0, 0));

            for (int i = 0; i < vehicles.Total; i++)
            {
                CvRect r = vehicles[i].Value.Rect;

                int cX = Cv.Round(r.X + r.Width * 0.5);
                int cY = Cv.Round(r.Y + r.Height * 0.5);
                int radius = Cv.Round((r.Width + r.Height) * 0.25);

                //Cv.DrawCircle(haarvehicle, new CvPoint(cX, cY), radius, CvColor.Red, 3);
                Cv.DrawRect(haarvehicle, r, CvColor.Red, 5);
            }

            return haarvehicle;
        }

        public void Dispose() //메모리 해제
        {
            if (haarvehicle != null) Cv.ReleaseImage(haarvehicle);
            if (gray != null) Cv.ReleaseImage(gray);

        }
    }
}
