#include <string.h>

int pin_count = 4;
int emg_pins[4] = {A0, A1, A2, A3};
unsigned long myTime;

// get analog value
int getAnalog(int pin, int repeat=32)
{
    long sum = 0;
    for(int i=0; i<repeat; i++)
    {
        sum += analogRead(pin);
    }
    
    int dta = sum/repeat;
    
    return dta;
}


void setup() {
  // put your setup code here, to run once:
  Serial.begin(9600);
  // Serial.println("starting");
}

void loop() {
  // put your main code here, to run repeatedly:
  String data = "";
  myTime = millis();
  data += String(myTime) + ", ";

  for(int i=0;i<pin_count;i++){
    int result = getAnalog(emg_pins[i]);
    data += String(result) + ", ";
  }
  Serial.println(data);
}
