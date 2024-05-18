# HeadTurner

Unity Source Code for HeadTurner (CHI 2025)

---

## Formative Study
### Task1
每一名受試者的每個姿勢執行一次  
**input:**  
participant's ID  
posture  
8 directions

**output:**  
`Formative_T1_[Participant]_[Posture].csv`  
| Participant | Posture | Direction | MaxViewingRange |
| - | - | - | - |
| 1 | Standing | 180 | 90 |

---

### Task2 (Observation1)
每一名受試者的每個姿勢執行八次 (四個測試跑一次)  
**input:**  
participant's ID  
posture  
4 direction-range pairs

**output:**  
`Formative_O1_[Participant]_[Posture].csv`  

- `time`: 從測試開始經過的時間 unit=seconds
- `Status`: 當前狀態，`non-testing` or `Direction A / Range B`
> 以下角度的計算方式為:  
> `object.transform.forward` 為從 (0, 0, 0) 為起點，方向為目前 object 面朝方向的單位向量  
> 將該向量的終點的 (x, y, z) 換算成以 z軸為正前方、x軸為正左方、y軸為正上方 的球座標 (radius, polar, azimuth)  
> [球座標解釋](https://zh.wikipedia.org/zh-tw/%E7%90%83%E5%BA%A7%E6%A8%99%E7%B3%BB)  

- 頭部(頭盔): `HeadPolar`, `HeadAzimuth`
- 肩膀: `ShoulderPolar`, `ShoulderAzimuth`
- 左眼視線: `LeftGazePolar`, `LeftGazeAzimuth`
- 右眼視線: `RightGazePolar`, `RightGazeAzimuth`

---

### Motion Platform (Shoulder Rotation)
`DOF Reality 控制箱關關先On`  
* **DOFCommunication:**  跟控制箱以SerialPort溝通，直接命令兩顆馬達的轉角  
* PortName：Windows電腦裡顯示為COM號，通常不曉得的時候會先開Arduino IDE的Tool看一下有/沒插USB哪個COM會出現/消失來判定
* Baud Rate：溝通頻率=500000
* Target Motor：分成Left跟Right，目前寫成public作為API，其他物件可以寫入，DOFCommunication物件會以Lerp形式以Motor Speed的速率命令馬達轉到目標
* Motor Speed：可調的轉速，Lerp在每次Update中會以Motor Speed*deltaTime的速率將馬達指令逼近Target Motor，實測1會偏慢或怪怪的，5以上似乎可行
