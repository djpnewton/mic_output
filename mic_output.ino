/*
  Analog input, serial output
 
 Reads an analog input pin, maps the result to a range from 0 to 255
 and prints the results to the serial monitor.
 
 The circuit:
 * freetronics sound input module with SPL connected to analog pin 0.
 * freetronics sound input module with SPL connected to analog pin 2.
 */

#include <ADC.h>
 
// These constants won't change.  They're used to give names
// to the pins used:
const int spl_l_pin = A9;  // ADC0
const int spl_r_pin = A3;  // ADC1
 
int spl_l = 0;
int spl_r = 0;

ADC* adc = new ADC();
elapsedMicros time;

const int SAMPLE_COUNT = 1.0;
const int PEAK_THRESH = 160;
const int PEAK_RELEASE = 20;
const int BUFSIZE = 1000;
struct peak_t {
  uint16_t value;
  unsigned long micros;
};
struct buf_t {
  uint8_t buf[BUFSIZE];
  int write_idx;
  float running_avg;
  struct peak_t peak;
  bool has_peak;
  bool peak_valid;
};

buf_t buf_l = {0};
buf_t buf_r = {0};
 
void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(spl_l_pin, INPUT);
  pinMode(spl_r_pin, INPUT);
    
  Serial.begin(115200); 

/**
  ///// ADC0 ////
  adc->setAveraging(1);
  adc->setResolution(8);
  adc->setConversionSpeed(ADC_VERY_HIGH_SPEED);
  adc->setSamplingSpeed(ADC_VERY_HIGH_SPEED);

  ////// ADC1 /////
  adc->setAveraging(1, ADC_1);
  adc->setResolution(8, ADC_1);
  adc->setConversionSpeed(ADC_VERY_HIGH_SPEED, ADC_1);
  adc->setSamplingSpeed(ADC_VERY_HIGH_SPEED, ADC_1);

  // interrup driven
  //adc->enableInterrupts(ADC_0);
  //adc->startContinuous(spl_l_pin, ADC_0);
  //adc->enableInterrupts(ADC_1);
  //adc->startContinuous(spl_r_pin, ADC_1);

  /// OR

  // mainloop driven
  adc->startSynchronizedContinuous(spl_l_pin, spl_r_pin);
**/

  ///// ADC0 ////
  adc->setAveraging(10);
  adc->setResolution(16);
  adc->setConversionSpeed(ADC_LOW_SPEED);
  adc->setSamplingSpeed(ADC_LOW_SPEED);
  adc->startContinuous(spl_l_pin, ADC_0);

  ////// ADC1 /////
  adc->setAveraging(10, ADC_1);
  adc->setResolution(16, ADC_1);
  adc->setConversionSpeed(ADC_LOW_SPEED, ADC_1);
  adc->setSamplingSpeed(ADC_LOW_SPEED, ADC_1);
  adc->startContinuous(spl_r_pin, ADC_1);


  delay(500);  
  Serial.println("end setup");
}

void update_buf(struct buf_t* buf, unsigned long mics, uint16_t new_val) {
  buf->running_avg = buf->running_avg + (new_val - buf->running_avg) / SAMPLE_COUNT;
  if (!buf->has_peak) {
    if (buf->running_avg > PEAK_THRESH) {
      buf->has_peak = true;
      buf->peak_valid = true;
      buf->peak.micros = mics;
      buf->peak.value = buf->running_avg;
      //digitalWriteFast(LED_BUILTIN, !digitalReadFast(LED_BUILTIN));
    }
  }
  else {
    if (buf->running_avg < PEAK_RELEASE)
      buf->has_peak = false;
    if (buf->peak.micros < mics - 5000)
      buf->peak_valid = false;    
  }
}

ADC::Sync_result result;
int count = 0;
int next_millis = 0;

void checkBufs(bool* peaks, struct peak_t* pl, struct peak_t* pr) {
  *pl = buf_l.peak;
  *pr = buf_r.peak;
  if (buf_l.has_peak && buf_l.peak_valid && buf_r.has_peak && buf_r.peak_valid) {
    buf_l.peak_valid = false;
    buf_r.peak_valid = false;
    *peaks = true;
  }  
}

void checkPeaks(int _millis) {
  bool peaks = false;
  peak_t pl, pr;
  //noInterrupts();
  checkBufs(&peaks, &pl, &pr);
  //interrupts();
  if (peaks) {    
    digitalWriteFast(LED_BUILTIN, !digitalReadFast(LED_BUILTIN));
    char buffer[50];
    sprintf(buffer, "P,%d,%d,%d,%d,%d\n", _millis, (int)pl.micros, (int)pl.value, (int)pr.micros, (int)pr.value);
    Serial.print(buffer);
  }
  count++;
}

void checkTime(int _millis) {
  if (_millis >= next_millis)
  {
    next_millis += 1000;
    char buffer[50];
    sprintf(buffer, "T,%d,%d,%d,%d\n", _millis, count, (int)buf_l.running_avg, (int)buf_r.running_avg);
    Serial.print(buffer);
  }  
}

void loop() {
  int _millis = millis();

/**
  // loop version
  result = adc->readSynchronizedContinuous();
  count++;
  
  unsigned long mics = micros();
  update_buf(&buf_l, mics, result.result_adc0);
  update_buf(&buf_r, mics, result.result_adc1);

  checkPeaks(_millis);
  checkTime(_millis);
**/

  // debug guff
  /*
  Serial.print(time, DEC);
  Serial.print(" ");
  Serial.print(count, DEC);
  Serial.print(" ");
  Serial.print(result.result_adc0, DEC);
  Serial.print(" ");
  Serial.println(result.result_adc1, DEC);
  */

  // old stuff
  spl_l = (uint16_t)adc->analogReadContinuous(ADC_0);
  spl_r = (uint16_t)adc->analogReadContinuous(ADC_1);            
 
  // print the results to the serial monitor:
  char buffer[20];
  sprintf(buffer, "R,%d,%05d,%05d\n", _millis, spl_l, spl_r);
  Serial.print(buffer); 

  delay(10);
}

void adc0_isr(void) {
  uint16_t val = (uint16_t)adc->analogReadContinuous(ADC_0);
  update_buf(&buf_l, micros(), val);

  int _millis = millis();
  checkPeaks(_millis);
  checkTime(_millis);
}
void adc1_isr(void) {
  uint16_t val = (uint16_t)adc->analogReadContinuous(ADC_1);
  update_buf(&buf_r, micros(), val);

  int _millis = millis();
  checkPeaks(_millis);
  checkTime(_millis);
}

