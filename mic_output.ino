/*
  Analog input, serial output
 
 Reads an analog input pin, maps the result to a range from 0 to 255
 and prints the results to the serial monitor.
 
 The circuit:
 * freetronics sound input module with SPL connected to analog pin 0.
 * freetronics sound input module with SPL connected to analog pin 2.
 */
 
// These constants won't change.  They're used to give names
// to the pins used:
const int splAdummy = A0;
const int splA1 = A4;  // Analog input pin that mic 1 SPL output is connected to
const int splA2 = A5;  // Analog input pin that mic 2 SPL output is connected to
 
int spl1 = 0;
int spl2 = 0;
 
void setup() {
  // initialize serial communications at 115200 bps:
  Serial.begin(115200); 
}
 
void loop() {
  // read the analog in values:
  analogRead(splA2);            
  delay(10);            
  spl2 = analogRead(splA2);            
  analogRead(splA1);
  delay(10);            
  spl1 = analogRead(splA1);
 
  // print the results to the serial monitor:
  char buffer[20];
  sprintf(buffer, "%04d,%04d\n", spl1, spl2);
  Serial.print(buffer);      
 
  //delay(100);
}

