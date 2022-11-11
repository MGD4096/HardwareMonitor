#include <SPI.h>
#include <Wire.h>
#include <DHT12.h>
#include <Adafruit_GFX.h>    // Core graphics library
#include <Adafruit_ST7789.h> // Hardware-specific library for ST7789
#include <XPT2046_Touchscreen.h>

#define SCREEN_WIDTH 240 // OLED display width, in pixels
#define SCREEN_HEIGHT 320 // OLED display height, in pixels

#define DHTPIN A5 // OLED display height, in pixels
#define DHTTYPE DHT11   // DHT 11
DHT12 DHT;

#define TFT_CS        10
#define TFT_RST        -1 // Or set to -1 and connect to Arduino RESET pin
#define TFT_DC         9
#define TCS_PIN  7

Adafruit_ST7789 tft = Adafruit_ST7789(TFT_CS, TFT_DC, TFT_RST);
XPT2046_Touchscreen ts(TCS_PIN); 

String currentScreen = "";
String inData;


void setup() {
  pinMode(8,OUTPUT);
  digitalWrite(8,HIGH);

  Serial.begin(115200);
  DHT.begin();

  tft.init(240, 320); 
  tft.fillScreen(ST77XX_BLACK);
  tft.setRotation(2);
  tft.drawLine(0, 25, 240, 25, 0x7BEF);
  tft.drawLine(65, 0, 65, 25, 0x7BEF);
  tft.drawLine(170, 0, 170, 25, 0x7BEF);

  ts.begin();
  ts.setRotation(2);

  currentScreen = "monitor";
}

void loop() {
  GetSerialData();
  
  if (ts.touched()) {
     setScreen(ts.getPoint());
  }

  drawtext(readTemp(), 0xBDF7, 2, 4, 2);
  drawtext(readHum(), 0xBDF7, 178, 4, 2);
  
}

void GetSerialData(){
  while (Serial.available() > 0)
    {
        char recieved = Serial.read();
        inData += recieved;
        if (recieved == '*')
        {
            inData.remove(inData.length() - 1, 1);
            //Serial.println(inData);
            drawtext(split(inData, ';', 0), 0xBDF7, 90, 4, 2);
            if(currentScreen.equals("switches")){
              drawSwitches();
            }
            else if(currentScreen.equals("monitor") && split(inData, ';', 1).equals("m")){
              drawMonitor(inData);
            }
            inData = ""; 
        }
  }
}

void drawtext(String text, uint16_t color, int16_t x, int16_t y, int8_t size) {
  tft.setCursor(x, y);
  tft.setTextSize(size);
  tft.setTextColor(color, ST77XX_BLACK);
  tft.setTextWrap(true);
  tft.print(text);
}

//draw switches screen
void drawSwitches(){
  tft.drawLine(120, 26, 120, 235, 0x7BEF);
  drawtext("POWER OFF", 0xBDF7, 6, 50, 2);
  drawtext("REBOOT", 0xBDF7, 150, 50, 2);
  tft.drawLine(0, 95, 240, 95, 0x7BEF);
  drawtext("LOCK", 0xBDF7, 35, 125, 2);
  drawtext("LOG OFF", 0xBDF7, 142, 125, 2);
  tft.drawLine(0, 165, 240, 165, 0x7BEF);
  drawtext("SLEEP", 0xBDF7, 30, 195, 2);
  drawtext("MONITOR", 0xBDF7, 142, 195, 2);
  tft.drawLine(0, 235, 240, 235, 0x7BEF);
}
 //draw monitor screen
void drawMonitor(String data){
  tft.drawLine(120, 26, 120, 225, 0x7BEF);
  //draw cpu temp
  int cpuTemp = split(data, ';', 3).toInt();
  uint16_t cpuTempColor = 0xBDF7;
  if(cpuTemp>60){
    cpuTempColor = 0xFBE0;
  }
  if(cpuTemp>80){
    cpuTempColor = 0xF800;
  }
  drawtext(getMValue(cpuTemp,"C"), cpuTempColor, 35, 40, 3);
  drawtext("CPU Temp", 0xBDF7, 12, 70, 2);
  //draw cpu load
  drawtext(getMValue(split(data, ';', 2).toInt(),"%"), 0xBDF7, 160, 40, 3);
  drawtext("CPU Load", 0xBDF7, 135, 70, 2);
  //draw bottom line
  tft.drawLine(0, 95, 240, 95, 0x7BEF);
  //draw GPU temp
  int gpuTemp = split(data, ';', 4).toInt();
  uint16_t gpuTempColor = 0xBDF7;
  if(gpuTemp>60){
    gpuTempColor = 0xFBE0;
  }
  if(gpuTemp>80){
    gpuTempColor = 0xF800;
  }
  drawtext(getMValue(gpuTemp,"C"), gpuTempColor, 35, 105, 3);
  drawtext("GPU Temp", 0xBDF7, 12, 135, 2);
  drawtext(getMValue(split(data, ';', 5).toInt(),"%"), 0xBDF7, 160, 105, 3);
  drawtext("GPU Load", 0xBDF7, 135, 135, 2);
  tft.drawLine(0, 160, 240, 160, 0x7BEF);
  //RAM
  drawtext(getMValue(split(data, ';', 6).toInt(),"%"), 0xBDF7, 35, 170, 3);
  drawtext("RAM", 0xBDF7, 42, 200, 2);
  //VRAM
  drawtext(getMValue(split(data, ';', 7).toInt(),"%"), 0xBDF7, 160, 170, 3);
  drawtext("VRAM", 0xBDF7, 162, 200, 2);
  tft.drawLine(0, 225, 240, 225, 0x7BEF);
  //POWER CONSUMPTION
  drawtext("POWER CONSUMPTION", 0xBDF7, 20, 235, 2);
  drawtext(getMValue(split(data, ';', 8).toInt(),"W"), 0xBDF7, 88, 265, 3);
}

//format monitor value
String getMValue(int value, String unit){
  int places = 2;
  String valueString = "";
  char buffor[4] = "    ";
  char format[4] = "%02i";
  if(value>99){
    places = 3;
  }
  format[2] = String(places).charAt(0);
  snprintf(buffor, places+1, format, value);
  valueString = String(buffor)+ unit;
  //clear double unit
  if(value<99){
    valueString = valueString + " ";
  }
  return valueString;
}

//change current screen
void setScreen(TS_Point p){
  if(currentScreen == "switches"){
    if(p.x>0 && p.y>500 && p.x<1920 && p.y<1100){
      Serial.println("command;reboot");
    }else if(p.x>1920 && p.y>500 && p.x<4000 && p.y<1100){
      Serial.println("command;shutdown");
    }else if(p.x>0 && p.y>1100 && p.x<1920 && p.y<1900){
      Serial.println("command;logoff");
    }else if(p.x>1920 && p.y>1100 && p.x<4000 && p.y<1900){
      Serial.println("command;lock");
    }else if(p.x>0 && p.y>1900 && p.x<1920 && p.y<2700){
      tft.fillRect(0, 26 , 240, 320, ST77XX_BLACK);
      currentScreen = "monitor";
    }else if(p.x>1920 && p.y>1900 && p.x<4000 && p.y<2800){
      Serial.println("command;sleep");
    }
    return;
  }
  if(currentScreen == "monitor"){
    if(p.x>2000){
      tft.fillRect(0, 26 , 240, 320, ST77XX_BLACK);
      currentScreen = "switches";
      return;
    }
    return;
  }
}

//Temperature sensor
String readTemp(){
  float t = DHT.readTemperature();
  return String(t, 1) + "C";
}

String readHum(){
  float h = DHT.readHumidity();
  return String(h, 1) + "%";
}

//split string
String split(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}