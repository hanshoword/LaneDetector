## LaneDetector ( 차선 인식 프로그램 )

[YOUTUBE LINK](https://www.youtube.com/watch?v=INyIDxLUrZw)

<img src="https://user-images.githubusercontent.com/47768726/59991847-ec3fd800-9683-11e9-8928-b855678710f0.JPG" width = "70%" height = "70%"></img>

## 프로젝트 목적
* OpenCV를 이용하여 차선 인식을 수행합니다.
* Canny edge, Hough Transform를 사용하여 직선인식을 구현합니다.
* 인식된 영상을 실시간으로 보여줘 사용자에게 편리함을 제공합니다.

## 프로젝트 구성요소 및 제한요소
  * Opencv 핵심 지식을 기반으로 Canny Edge, Hough Transform, Filter 기능 설계 및 구현
  * Filter는 이진화와 HSV 및 RGB 공간에서 색상 검출이 이루어짐
  * 관심영역 ROI(Region of interesting) 구현
  * 영상파일 및 Cam을 통해 실시간 영상을 받아들이는 부분 구현

  * 곡선인식 제한
  * 도로의 환경에따라 인식에 영향을 받음 ( 그림자 및 햇빛 ) 

## 구현 사양
* Visual studio 2017 (Window 10)
* OpencvSharp
* C# (Winform)

## Sequence Diagram

<img src="https://user-images.githubusercontent.com/47768726/59991334-f6f96d80-9681-11e9-8b87-6976df4c35c8.jpg" width="90%"></img>

* PipeLine
   * Video를 캡쳐로 받아와, timer를 통해 프레임별로 처리할수있도록 합니다.
   * Slice Image(ROI)를 통해 관심영역을 설정합니다. 
      * src와 동일한 크기의 검은색 mask를 생성합니다. 
      * src를 좌하,좌상,우상,우하를 기준으로 관심영역을 잘라낸 후 mask와 합치면 됩니다.
   * Yellow추출은 Hsv공간에서, White추출은 Rgb공간을 이용합니다.
      * Yellow Range : 10, 75, 75 ~ 56, 255, 255
      * White Range : 0, 200, 10 ~ 255, 255, 255
   * 색상추출된 이미지를 가우시안 블러를 통해 잡음을 제거하며, 임계점에 따른 Binary로 흰색,검은색으로 이루어진 이미지를 얻습니다.
   * CannyEdge를 사용해 차선의 윤곽선을 검출합니다.
     * Canny란 이미지의 가장자리(윤곽선)을 찾아서 반환해줍니다.
     * 최솟값과 최댓값을 사용하여 가장자리를 검출합니다
     * 최솟값 이하의 값은 가장자리로 간주하지않고, 최댓값이상의 값에 포함된 가장자리를 가장자리로 간주합니다.
     * 만약 최솟값:100, 최댓값:200이라면 100이하는 모두제외하며, 연결된 선중에 200이상의 값을 하나라도 포함하면 가장자리로 간주합니다.
   * 마지막으로 Probabilistic Hough를 사용하여 윤곽선에서 직선을 검출합니다. 
     * 확률적 허프변환 : 모든 픽셀에 대해 변환을 수행하지않고 무작위로 픽셀을 선택하여 허프변환 수행
     * probabilistic : 시작점, 끝점을 반환     
       * parameter1 : 최소선의 길이 : 이 값보다 작으면 reject
       * parameter2 : 최대선 간격 : 선과 선사이의 최대 허용간격, 이 값보다 작으면 reject


## Source Code 목록
  ### Class
  #### LaneDetection Class
  ##### Methods
  * public IplImage BaseBinary(IplImage src, int threshold)
  <img src="https://user-images.githubusercontent.com/47768726/59994961-b48b5d00-9690-11e9-8a05-9eb072faa2c3.JPG" width = "90%"></img>
    
     * 기본적인 이진화 변환 메서드입니다.  
   
     * Gray변환을 수행 한 후, 임계치를 매개변수로 받아와 이 임계점 이하라면 검은색, 임계점 이상이라면 흰색을 출력합니다.
        
    * public IplImage Binary(IplImage src, int threshold)
  
  <img src = "https://user-images.githubusercontent.com/47768726/59995926-cae6e800-9693-11e9-9ec7-f731354fbfab.JPG" width = "90%"></img>
  
    * Adaptive thresholding(적응형 경계화)를 통한 이진화 메서드입니다.
     
    * void cv::adaptiveThreshold( InputArray src, OutputArray dst, double maxValue, int adaptiveMethod, int thresholdType, int blocksize, double C)   
    
    * 고정된 경계값을 사용하면 부분적으로 이진화가 만족스럽게 되지 않을수도 있습니다.
    
    * 적응적 경계화를 실시 : 픽셀 위치마다 다른 threshold(이웃하는 픽셀값에 의해 결정)를 사용하는 가변적 기법 입니다.
         * b x b 크기의 블록을 설정후 가우시안 가중평균을 구합니다( 블록 중심에 가까울수록 높은 가중치를 주어 평균을 구하는 방법)
         * 평균값에서 상수 파라미터 C를 빼면 threshold값을 얻을수 있습니다.

    * public IplImage BlurImage(IplImage src)
  
  <img src = "https://user-images.githubusercontent.com/47768726/60001531-9b3edc80-96a1-11e9-9731-57dff878251e.JPG" width= "90%"></img>
  
    * Gaussian Blur를 사용합니다
    
    * BxB 필터를 크기에서 중앙의 인접한 값을 기준으로 필터중앙의 픽셀값을 재조정 하여 이미지의 노이즈를 제거합니다 
    
    
  * public IplImage SliceImage(IplImage src)
  <img src = "https://user-images.githubusercontent.com/47768726/60002115-00df9880-96a3-11e9-9722-c1dd8c431a6f.JPG" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60002118-0341f280-96a3-11e9-8c8c-d07b9cd1ef35.png" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60001568-b1e53380-96a1-11e9-9fc9-ea6cef4beed0.JPG" width= "90%"></img>
  
  * public IplImage YellowTransform(IplImage src)
  <img src = "https://user-images.githubusercontent.com/47768726/60001588-bd385f00-96a1-11e9-8ee3-4a18a6cb74a4.JPG" width= "90%"></img>
  * public IplImage WhiteTransform(IplImage src)
  <img src = "https://user-images.githubusercontent.com/47768726/60001598-bf9ab900-96a1-11e9-9df5-8a06ceac3076.JPG" width= "90%"></img>
  
    * yellow & white 병합
    <img src = "https://user-images.githubusercontent.com/47768726/60002147-0fc64b00-96a3-11e9-95e3-937dfcae0402.png" width= "90%"></img>

    * 검출된 차선의 이진화
    <img src = "https://user-images.githubusercontent.com/47768726/60002280-5156f600-96a3-11e9-9728-b4ecf557f5b3.png" width= "90%"></img>
  * public IplImage CannyLine(IplImage src, char color_line)
  <img src = "https://user-images.githubusercontent.com/47768726/60001608-c4f80380-96a1-11e9-92a1-6f6a3d430030.JPG" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60001614-c75a5d80-96a1-11e9-98a2-01514ec1b59e.JPG" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60001618-c9242100-96a1-11e9-9936-abd86ed22c3c.JPG" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60002062-ef968c00-96a2-11e9-99e1-cb0d0f55d8c0.png" width= "90%"></img>
  <img src = "https://user-images.githubusercontent.com/47768726/60002069-f1604f80-96a2-11e9-808a-becf24cf8137.png" width= "90%"></img>
  
  * public IplImage HoughLines(IplImage src)
  <img src = "https://user-images.githubusercontent.com/47768726/60002186-29679280-96a3-11e9-9642-9a78e58c2db9.png" width= "90%"></img>
  * public void Quick_Recursive(double[] data, int left, int right)
  
  * public void Quick_Sort(double[] data, int count)
  
  * public void Dispose() //메모리 해제
  
  ### Main

## 구현 결과물

## 분석 및 평가

## 개선 방안
