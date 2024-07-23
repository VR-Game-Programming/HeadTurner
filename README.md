# HeadTurner

Unity Source Code for HeadTurner (CHI 2025)

---

### Motion Platform (Shoulder Rotation)
`DOF Reality 控制箱關關先On`  
* **DOFCommunication:**  跟控制箱以SerialPort溝通，直接命令兩顆馬達的轉角  
* PortName：Windows電腦裡顯示為COM號，通常不曉得的時候會先開Arduino IDE的Tool看一下有/沒插USB哪個COM會出現/消失來判定
* Baud Rate：溝通頻率=500000
* Target Motor：分成Left跟Right，目前寫成public作為API，其他物件可以寫入，DOFCommunication物件會以Lerp形式以Motor Speed的速率命令馬達轉到目標
* Motor Speed：可調的轉速，Lerp在每次Update中會以Motor Speed*deltaTime的速率將馬達指令逼近Target Motor，實測1會偏慢或怪怪的，5以上似乎可行
