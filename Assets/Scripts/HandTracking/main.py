from cvzone.HandTrackingModule import HandDetector
import cv2
import socket

# 1. SETUP CAMERA
cap = cv2.VideoCapture(0)
cap.set(3, 640)  # Width
cap.set(4, 480)  # Height

# 2. SETUP DETECTOR
detector = HandDetector(detectionCon=0.8, maxHands=1)

# 3. SETUP UDP
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)

while True:
    # Get the image frame
    success, img = cap.read()
    h, w, _ = img.shape 
    
    # Find Hands
    hands, img = detector.findHands(img, draw=True) 
    
    data = []

    if hands:
        hand = hands[0]
        lmList = hand["lmList"]
        
        for lm in lmList:
            data.extend([lm[0], h - lm[1], lm[2]])

        # Send data to Unity
        sock.sendto(str.encode(str(data)), serverAddressPort)

    cv2.imshow("Image", img)
    cv2.waitKey(1)