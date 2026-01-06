from cvzone.HandTrackingModule import HandDetector
import cv2
import socket

cap = cv2.VideoCapture(0)
cap.set(3, 640)
cap.set(4, 480)

detector = HandDetector(detectionCon=0.1, maxHands=1)
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
serverAddressPort = ("127.0.0.1", 5052)

while True:
    success, img = cap.read()
    h, w, _ = img.shape 
    hands, img = detector.findHands(img, draw=True) 
    
    data = []

    if hands:
        hand = hands[0]
        lmList = hand["lmList"]
        
        for lm in lmList:
            data.extend([lm[0], h - lm[1], lm[2]])
        fingers = detector.fingersUp(hand)
        
        gesture_signal = -1
        if fingers == [0, 0, 0, 0, 0]: gesture_signal = 1
        elif fingers == [1, 1, 1, 1, 1]: gesture_signal = 0

        data.append(gesture_signal)
        # -------------------------

        sock.sendto(str.encode(str(data)), serverAddressPort)

    cv2.imshow("Image", img)
    cv2.waitKey(1)