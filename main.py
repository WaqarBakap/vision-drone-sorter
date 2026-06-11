import cv2
import numpy as np
import socket
import time
# Load the captured image (substitute with your actual image path)
image_path = r"C:\Unity\card_screenshot.png"
image = cv2.imread(image_path)

# Convert BGR image to HSV
hsv_image = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)

# Define color ranges for red, green, and blue
lower_red = np.array([0, 100, 100])
upper_red = np.array([10, 255, 255])

lower_green = np.array([35, 100, 100])
upper_green = np.array([85, 255, 255])

lower_blue = np.array([100, 100, 100])
upper_blue = np.array([130, 255, 255])

lower_red1 = np.array([0, 100, 100])
upper_red1 = np.array([10, 255, 255])
lower_red2 = np.array([170, 100, 100])
upper_red2 = np.array([180, 255, 255])
mask_red1 = cv2.inRange(hsv_image, lower_red1, upper_red1)
mask_red2 = cv2.inRange(hsv_image, lower_red2, upper_red2)

mask_red = mask_red1 + mask_red2

# Create masks for each color
mask_red = cv2.inRange(hsv_image, lower_red, upper_red)
mask_green = cv2.inRange(hsv_image, lower_green, upper_green)
mask_blue = cv2.inRange(hsv_image, lower_blue, upper_blue)

# Combine the masks to reduce false positives
combined_mask = mask_red + mask_green + mask_blue

# Find contours in the combined mask
contours, _ = cv2.findContours(combined_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

# Initialize counters for red, green, and blue cards
red_count = 0
green_count = 0
blue_count = 0

# Initialize color array
colors = []

# Draw bounding rectangles around detected cards with respective colors
for contour in contours:
    x, y, w, h = cv2.boundingRect(contour)
    area = cv2.contourArea(contour)
    if area > 100:  # Adjust the area threshold as needed
        if np.any(mask_red[contour[0][0][1], contour[0][0][0]]):
            cv2.rectangle(image, (x, y), (x + w, y + h), (0, 0, 255), 2)  # Red
            red_count += 1
            colors.append('red')
        elif np.any(mask_green[contour[0][0][1], contour[0][0][0]]):
            cv2.rectangle(image, (x, y), (x + w, y + h), (0, 255, 0), 2)  # Green
            green_count += 1
            colors.append('green')
        elif np.any(mask_blue[contour[0][0][1], contour[0][0][0]]):
            cv2.rectangle(image, (x, y), (x + w, y + h), (255, 0, 0), 2)  # Blue
            blue_count += 1
            colors.append('blue')

# Save the image with bounding rectangles
cv2.imwrite("detected_cards_colored.png", image)

print("Image from unity processed")

# Print the counts
print(f"Red cubes: {red_count}")
print(f"Green cubes: {green_count}")
print(f"Blue cubes: {blue_count}")

# Function to move the drone in Unity based on color
def move_drone(color):
    # Connect to Unity server
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.connect(('localhost', 65431))
        s.sendall(color.encode())
        data = s.recv(1024)
        print(f'Received {data.decode()} from Unity')
        time.sleep(20)  # Added a delay of 20 seconds (adjustable as needed)
print(colors)
# Main loop
for color in colors:
    move_drone(color)
    time.sleep(20)  # Added a delay of 20 seconds (adjustable as needed) after each box is picked up and dropped