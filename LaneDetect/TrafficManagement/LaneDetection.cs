using System;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;
using OpenCvSharp.Extensions;
using System.Collections.Generic;

namespace TrafficManagement
{

    /* HoughLines2함수를 사용하여 직선 검출
     * 1) 이진화 (Binary) 처리
     * 2) 모폴로지 (Morphology) 처리(팽창(Dilate), 침식(Erode))
     * 3) 캐니엣지 (Canny) 처리
     * 
     * HoughLines2 검출방법 : 모든 점들의 직선의 방정식을 생성
     * 이 직선의 방정식이 교차하는 점이 가장 많은 점을 기준으로 직선으로 판별
     * x = rho cos theta = x0
     * y = rho sin theta = y0
     * beta = scale * sin theta
     * alpha = scale * cos theta
     * 
     * standard : rho, theta 반환
     * probabilistic : 시작점, 끝점 반환
     * rho = 0~1
     * theta = 라디안 범위 [pi/180을 고정적으로 사용]
     * 임계값 : 검출 기준, 낮으면 낮은선도 검출, 높으면 정확도 높은선만 검출
     * parameter1 : standard (사용안함,0) probabilistic (최소선의 길이) multiscale (rho의 나눔수 [rho/parameter1])
     * parameter2  : standard (사용안함,0) probabilistic ( 최대선 간격) multiscale (theta의 나눔수 [theta / parameter2])
     */


    /* Canny Edge
     * 이미지에서 가장자리를 찾아 반환
     * 임계값1(최솟값), 임계값2(최댓값)을 사용하여 가장자리 검출
     * 최솟값 이하의 값은 가장자리로 간주하지않음,
     * 최댓값이상의 값에 포함된 가장자리는 가장자리로 간주
     * 연결된 가장자리에서 최댓값에 포함될경우 가장자리로 간주
     * 임1 : 100, 임2 : 200
     * 100이하 모두제외
     * 연결된 선중에서 200이상의 값을 하나라도 포함할경우 가장자리로 간주
     */

    class LaneDetection : IDisposable
    {
        VehicleDetection VDConvert = new VehicleDetection();

        IplImage bin;
        IplImage houline;
        IplImage hsv;
        IplImage hls;
        IplImage slice;
        IplImage blur;
        IplImage bitwise;
        IplImage combine;
        IplImage yellow;
        IplImage white;
        IplImage mask;
        IplImage input1;
        IplImage input2;
        IplImage output;

        static CvLineSegmentPoint prev_elemLeft = new CvLineSegmentPoint();
        static CvLineSegmentPoint prev_elemRight = new CvLineSegmentPoint();
        int flag_yellow;
        int flag_white;
        CvPoint2D64f P1 = new CvPoint2D64f();
        CvPoint2D64f P2 = new CvPoint2D64f();

        IplImage vehicle_detection;

        double d_dx;
        double d_dy;

        public IplImage Binary(IplImage src, int threshold) //이진화 메서드
        {
            bin = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, bin, ColorConversion.RgbToGray);

            Mat m_bin = new Mat(bin);
            Cv2.AdaptiveThreshold(m_bin, m_bin, 255, AdaptiveThresholdType.GaussianC, ThresholdType.Binary, 21, 10);
            bin = m_bin.ToIplImage();

            return m_bin.ToIplImage();
        }

        public IplImage BaseBinary(IplImage src, int threshold) //이진화 메서드
        {
            bin = new IplImage(src.Size, BitDepth.U8, 1);
            Cv.CvtColor(src, bin, ColorConversion.RgbToGray);
            Cv.Threshold(bin, bin, threshold, 255, ThresholdType.Binary); //임계점 이하면 흑색, 임계점 이상이면 백색 150은 임계값, 255는 최대값
            
            return bin;
        }

        public IplImage BlurImage(IplImage src) //가우시안 블러
        {
            blur = new IplImage(src.Size, BitDepth.U8, 3);
            Cv.Smooth(src, blur, SmoothType.Gaussian, 5);

            return blur;
        }

        public IplImage SliceImage(IplImage src) //일정영역 제한
        {
            slice = new IplImage(src.Size, BitDepth.U8, 3);
            mask = new IplImage(src.Size, BitDepth.U8, 3);

            mask = src.Clone();

            Mat input1 = new Mat(mask);
            Mat bitwise = new Mat();
            
            CvPoint[][] points = new CvPoint[2][];
            for(int i=0; i<2; i++)
            {
                points[i] = new CvPoint[4];
            }

            points[0][0] = new CvPoint(0, src.Height * 95 / 100); //좌하
            points[0][3] = new CvPoint(src.Width, src.Height * 95 / 100); //우하
            points[0][1] = new CvPoint(src.Width * 3 / 7, src.Height * 5 / 8); //좌상
            points[0][2] = new CvPoint(src.Width * 4 / 7, src.Height * 5 / 8); //우상
                                          
            Cv.FillPoly(slice, points, CvColor.White);
            Mat input2 = new Mat(slice);
            Cv2.BitwiseAnd(input1, input2, bitwise);            

            return bitwise.ToIplImage(); 
        }


        public IplImage YellowTransform(IplImage src) // hsv: 노란색 검출, rgb : 흰색 검출
        {
            hsv = new IplImage(src.Size, BitDepth.U8, 3);

            yellow = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, hsv, ColorConversion.BgrToHsv);

            Cv.InRangeS(hsv, new CvScalar(10, 75, 75), new CvScalar(56, 255, 255), yellow); //hsv공간 노랑선

            hsv.SetZero();

            Cv.Copy(src, hsv, yellow);            

            return hsv;
        }
        public IplImage WhiteTransform(IplImage src) // hsv: 노란색 검출, rgb : 흰색 검출
        {
            hls = new IplImage(src.Size, BitDepth.U8, 3);

            white = new IplImage(src.Size, BitDepth.U8, 1);

            Cv.CvtColor(src, hls, ColorConversion.BgrToHls);

            Cv.InRangeS(hls, new CvScalar(0, 200, 10), new CvScalar(255, 255, 255), white); //hls공간 흰색선

            hls.SetZero();

            Cv.Copy(src, hls, white);

            return hls;
        }

        public IplImage CannyLine(IplImage src, char color_line) //허프를 통한 차선 검출
        {
            slice = this.SliceImage(src);
            hsv = this.YellowTransform(slice);
            hls = this.WhiteTransform(slice);
            if (color_line == 'y')
            {
                blur = this.BlurImage(hsv);
                bin = this.Binary(blur, 50); //Adaptive Thresholding로 변환
                Cv.Canny(bin, bin, 50, 200, ApertureSize.Size3);
                return bin;
            }
            else if (color_line == 'w')
            {
                blur = this.BlurImage(hls);
                bin = this.Binary(blur, 50); //Adaptive Thresholding로 변환
                Cv.Canny(bin, bin, 50, 200, ApertureSize.Size3);
                return bin;
            }
            else
                return null;
        }

        public IplImage HoughLines(IplImage src) //허프를 통한 차선 검출
        {
            flag_yellow = 0;
            flag_white = 0;

            slice = this.SliceImage(src);
            hsv = this.YellowTransform(slice);
            hls = this.WhiteTransform(slice);

            blur = this.BlurImage(hsv);
            bin = this.Binary(hsv, 50); //Adaptive Thresholding로 변환
            Cv.Canny(bin, bin, 50, 200, ApertureSize.Size3);

            CvMemStorage Storage = new CvMemStorage();  

            /* Probabilistic 검출*/
            CvSeq lines = bin.HoughLines2(Storage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, 50, 100, 100); 
            
            double[] LineAngle = new double[lines.Total];
            double[] LineAngle_q = new double[lines.Total];

            if(lines != null)
            {
                for (int i = 0; i < lines.Total; i++) //검출된 모든 라인을 검사
                {
                    CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value; // 해당 라인의 데이터를 가져옴

                    int dx = elem.P2.X - elem.P1.X; //x좌표의 차
                    int dy = elem.P2.Y - elem.P1.Y; //y좌표의 차
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI; //기울기 구하기

                    LineAngle[i] = angle;
                    LineAngle_q[i] = angle;
                }

                if (lines.Total != 0)
                    Quick_Sort(LineAngle_q, lines.Total);

                for (int i = 0; i < lines.Total; i++)
                {
                    CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value; // 해당 라인의 데이터를 가져옴

                    int dx = elem.P2.X - elem.P1.X; //x좌표의 차
                    int dy = elem.P2.Y - elem.P1.Y; //y좌표의 차
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI; //기울기 구하기

                    if (LineAngle_q[lines.Total - 1] == LineAngle[i] || LineAngle_q[0] == LineAngle[i])
                    {
                        //P2에 50을 더해도 P1보다 작거나, P2에 50을 빼도 P1보다 큰경우
                        if (elem.P1.Y > elem.P2.Y || elem.P1.Y < elem.P2.Y) //P1 :시작점, P2 : 도착점
                        {

                            if (Math.Abs(angle) >= 14 && Math.Abs(angle) <= 80)
                            {
                                Cv.PutText(src, "Yellow angle : " + angle.ToString(), new CvPoint(100,50), new CvFont(FontFace.HersheyComplex, 1, 1), CvColor.Yellow);
                                P1 = elem.P1;
                                P2 = elem.P2;
                                d_dx = Math.Abs(P2.X - P1.X);
                                d_dy = Math.Abs(P2.Y - P1.Y);

                                while (true)
                                {
                                    if (P1.Y > P2.Y)
                                    {
                                        if (P1.Y < src.Height)
                                        {
                                            if (P1.X < P2.X)
                                            {
                                                P1.X -= d_dx / 100;
                                                P1.Y += d_dy / 100;
                                            }
                                            else
                                            {
                                                P1.X += d_dx / 100;
                                                P1.Y += d_dy / 100;
                                            }
                                        }
                                        else if (P2.Y > src.Height * 5 / 8)
                                        {
                                            if (P1.X > P2.X)
                                            {
                                                P2.X -= d_dx / 100;
                                                P2.Y -= d_dy / 100;
                                            }
                                            else
                                            {
                                                P2.X += d_dx / 100;
                                                P2.Y -= d_dy / 100;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                    }
                                    else
                                    {
                                        if (P2.Y < src.Height)
                                        {
                                            if (P1.X > P2.X)
                                            {
                                                P2.X -= d_dx / 100;
                                                P2.Y += d_dy / 100;
                                            }
                                            else
                                            {
                                                P2.X += d_dx / 100;
                                                P2.Y += d_dy / 100;
                                            }
                                        }
                                        else if (P1.Y > src.Height * 5 / 8)
                                        {
                                            if (P1.X < P2.X)
                                            {
                                                P1.X -= d_dx / 100;
                                                P1.Y -= d_dy / 100;
                                            }
                                            else
                                            {
                                                P1.X += d_dx / 100;
                                                P1.Y -= d_dy / 100;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }

                                    d_dx = Math.Abs(P2.X - P1.X);
                                    d_dy = Math.Abs(P2.Y - P1.Y);
                                }
                                flag_yellow = 1;
                                prev_elemLeft.P1 = P1;
                                prev_elemLeft.P2 = P2;

                                src.Line(P1, P2, CvColor.Yellow, 10, LineType.AntiAlias, 0); //P1,P2의 좌표를 가지고 사용자에게 정보를 줄수있음(ex) angle이 심하게 올라가면 차선이탈
                                break;
                            }
                        }
                    }
                }
            }            

            blur = this.BlurImage(hls);
            bin = this.Binary(hls, 50); //Adaptive Thresholding로 변환
            Cv.Canny(bin, bin, 50, 150, ApertureSize.Size3);

            /* Probabilistic 검출*/
            lines = bin.HoughLines2(Storage, HoughLinesMethod.Probabilistic, 1, Math.PI / 180, 50, 100, 100);
            LineAngle = new double[lines.Total];
            LineAngle_q = new double[lines.Total];

            if(lines != null)
            {
                for (int i = 0; i < lines.Total; i++) //검출된 모든 라인을 검사
                {
                    CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value; // 해당 라인의 데이터를 가져옴

                    int dx = elem.P2.X - elem.P1.X; //x좌표의 차
                    int dy = elem.P2.Y - elem.P1.Y; //y좌표의 차
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI; //기울기 구하기

                    LineAngle[i] = angle;
                    LineAngle_q[i] = angle;
                }

                if (lines.Total != 0)
                    Quick_Sort(LineAngle_q, lines.Total);

                for (int i = 0; i < lines.Total; i++)
                {
                    
                    CvLineSegmentPoint elem = lines.GetSeqElem<CvLineSegmentPoint>(i).Value; // 해당 라인의 데이터를 가져옴

                    int dx = elem.P2.X - elem.P1.X; //x좌표의 차
                    int dy = elem.P2.Y - elem.P1.Y; //y좌표의 차
                    double angle = Math.Atan2(dy, dx) * 180 / Math.PI; //기울기 구하기

                    if (LineAngle_q[lines.Total - 1] == LineAngle[i] || LineAngle_q[0] == LineAngle[i])
                    {
                        //P2에 50을 더해도 P1보다 작거나, P2에 50을 빼도 P1보다 큰경우
                        if (elem.P1.Y > elem.P2.Y || elem.P1.Y < elem.P2.Y) //P1 :시작점, P2 : 도착점
                        {
                            if (Math.Abs(angle) >= 14 && Math.Abs(angle) <= 80)
                            {
                                Cv.PutText(src, "Whtie angle : " + angle.ToString(), new CvPoint(100, 100), new CvFont(FontFace.HersheyComplex, 1, 1), CvColor.White);

                                P1 = elem.P1;
                                P2 = elem.P2;
                                d_dx = Math.Abs(P2.X - P1.X);
                                d_dy = Math.Abs(P2.Y - P1.Y);
                                while (true)
                                {
                                    if (P1.Y > P2.Y)
                                    {
                                        if (P1.Y < src.Height)
                                        {
                                            if (P1.X < P2.X)
                                            {
                                                P1.X -= d_dx / 100;
                                                P1.Y += d_dy / 100;
                                            }
                                            else
                                            {
                                                P1.X += d_dx / 100;
                                                P1.Y += d_dy / 100;
                                            }
                                        }
                                        else if (P2.Y > src.Height * 5 / 8)
                                        {
                                            if (P1.X > P2.X)
                                            {
                                                P2.X -= d_dx / 100;
                                                P2.Y -= d_dy / 100;
                                            }
                                            else
                                            {
                                                P2.X += d_dx / 100;
                                                P2.Y -= d_dy / 100;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }

                                    }
                                    else
                                    {
                                        if (P2.Y < src.Height)
                                        {
                                            if (P1.X > P2.X)
                                            {
                                                P2.X -= d_dx / 100;
                                                P2.Y += d_dy / 100;
                                            }
                                            else
                                            {
                                                P2.X += d_dx / 100;
                                                P2.Y += d_dy / 100;
                                            }
                                        }
                                        else if (P1.Y > src.Height * 5 / 8)
                                        {
                                            if (P1.X < P2.X)
                                            {
                                                P1.X -= d_dx / 100;
                                                P1.Y -= d_dy / 100;
                                            }
                                            else
                                            {
                                                P1.X += d_dx / 100;
                                                P1.Y -= d_dy / 100;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    d_dx = Math.Abs(P2.X - P1.X);
                                    d_dy = Math.Abs(P2.Y - P1.Y);
                                }
                                flag_white = 1;
                                prev_elemRight.P1 = P1;
                                prev_elemRight.P2 = P2;

                                src.Line(P1, P2, CvColor.White, 10, LineType.AntiAlias, 0); //P1,P2의 좌표를 가지고 사용자에게 정보를 줄수있음(ex) angle이 심하게 올라가면 차선이탈
                                break;

                                
                            }
                        }
                    }
                }
            }
            if (flag_yellow == 0)
            {
                src.Line(prev_elemLeft.P1, prev_elemLeft.P2, CvColor.Yellow, 10, LineType.AntiAlias, 0);
            }
    
            if(flag_white == 0)
            {
                src.Line(prev_elemRight.P1, prev_elemRight.P2, CvColor.White, 10, LineType.AntiAlias, 0);
            }

            Cv.ReleaseMemStorage(Storage);

            //vehicle_detection = this.VDConvert.VehicleDetect(src);
            Dispose();
            return src;
        }

        public void Quick_Recursive(double[] data, int left, int right)
        {
            double pivot;
            int left_hold, right_hold;
            left_hold = left;
            right_hold = right;

            pivot = data[left];
            while(left < right)
            {
                while((pivot <= data[right]) && (left < right))
                {
                    right--;
                }
                if(left != right)
                {
                    data[left] = data[right];
                }

                while ((data[left] <= pivot) && (left < right))
                {
                    left++;
                }
                if (left != right)
                {
                    data[right] = data[left];
                    right--;
                }
            }

            data[left] = pivot;
            pivot = left;
            left = left_hold;
            right = right_hold;

            if(left < pivot)
            {
                Quick_Recursive(data, left, (int)pivot - 1);
            }
            if (right > pivot)
            {
                Quick_Recursive(data, (int)pivot+1, right);
            }
        }

        public void Quick_Sort(double[] data, int count)
        {
            Quick_Recursive(data, 0, count - 1);
        }

        public void Dispose() //메모리 해제
        {
            if (bin != null) Cv.ReleaseImage(bin);
            if (hsv != null) Cv.ReleaseImage(hsv);
            if (hls != null) Cv.ReleaseImage(hls);
            if (houline != null) Cv.ReleaseImage(houline);
            if (slice != null) Cv.ReleaseImage(slice);
            if (blur != null) Cv.ReleaseImage(blur);
            if (combine != null) Cv.ReleaseImage(combine);
            if (yellow != null) Cv.ReleaseImage(yellow);
            if (white != null) Cv.ReleaseImage(white);
            if (mask != null) Cv.ReleaseImage(mask);
            if (input1 != null) Cv.ReleaseImage(input1);
            if (input2 != null) Cv.ReleaseImage(input2);
            if (output != null) Cv.ReleaseImage(output);
            if (vehicle_detection != null) Cv.ReleaseImage(vehicle_detection);
        }
    }
}
