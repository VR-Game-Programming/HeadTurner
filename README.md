# HeadTurner

Unity Source Code for HeadTurner (CHI 2025)

---

### Task1
每一名受試者的每個姿勢執行一次  
**input:**  
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
8 maximum viewing range (from task1，每個姿勢輸入一次就好)
4 direction-range pairs

**output:**  
`Formative_O1_[Participant]_[Posture].csv`  
| Participant | Posture | Direction | RangePercentage | ViewingRange | time | HeadRotationAngle | GazingAngle | ShoulderRotationAngle | 
| - | - | - | - | - | - | - | - | - |
| 1 |	Standing |	180	 | P100 |	100 |	11.76902 | 2.118864	| 0 | 1.229397 |

time: seconds from test starting
