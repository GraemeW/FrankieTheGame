from PIL import Image
import os

class TestImage:
    def __init__(self):
        self.outputPath = "Output"

    def MakeSinglePixel(self, r, g, b, a):
        image = Image.new(mode="RGBA", size=(1,1), color=(r,g,b,a))
        savePath = os.path.join(self.outputPath, "singlePix.png")
        image.save(savePath)

# Main Execution
if __name__ == "__main__":
    testImage = TestImage()
    testImage.MakeSinglePixel(0,0,0,0)
