# ObjectTracking
Human target detection and tracking

### 功能
* 可以对正面的人体目标进行实时跟踪，跟踪效果良好。

### 运行环境/硬件
* windows7、 Visual Studio 2013
* EmguCv2.4.9
* 小R科技-51duino WiFi视频智能小车机器人（更换加强版的路由模块）

### 核心算法

* 使用seetaFace进行人脸检测定位人体目标
* 使用改进的Camshift算法（形态学约束+重检测）进行跟踪
* 使用C#编写上位机并进行核心运动控制逻辑的编写

### 效果图
* 初始帧检测
![初始帧检测](https://github.com/Tachone/ObjectTracking/blob/master/%E5%9B%BE%E7%89%871.png)
* 直行跟踪
![直行跟踪](https://github.com/Tachone/ObjectTracking/blob/master/%E5%9B%BE%E7%89%872.png)
* 转弯跟踪
![转弯跟踪](https://github.com/Tachone/ObjectTracking/blob/master/%E5%9B%BE%E7%89%873.png)
* 机器人小车实物
![机器人小车实物](https://github.com/Tachone/ObjectTracking/blob/master/%E5%9B%BE%E7%89%874.png)

### 具体实现
