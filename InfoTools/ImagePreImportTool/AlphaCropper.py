from tkinter import image_names
from dataclasses import dataclass
from PIL import Image
import numpy as np
import os

# Data Structures
@dataclass
class NameImagePair:
    imageName : str
    image : Image

# Classes / Methods
class AlphaCropper:
    # Static Methods
    @staticmethod
    def AddPadding(image : Image.Image, left : int, right : int, top : int, bottom : int) -> Image.Image:
        paddedImage = Image.new(image.mode, (image.width + left + right, image.height + top + bottom), (0, 0, 0, 0))
        paddedImage.paste(image, (left, top))
        return paddedImage

    #Initialization
    def __init__(self) -> None:
        # Tunables
        self.inputPath = "Input"
        self.outputPath = "Output"
        self.outputExtension = "png"
        self.fileTypes = list([".png", ".PNG"])
        self.transparencyPadding = 1

        # State
        self.imagePathList:list[str] = list()
        self.nameImagePairs:list[NameImagePair] = list()

    # Public Methods
    def LoadFromDefault(self):
        self.__SetImagePaths()
        self.__SetImages()

    def CropImages(self):
        for nameImagePair in self.nameImagePairs:
            # Get Image
            print(f'On image: {nameImagePair.imageName}')
            image = nameImagePair.image
            imageArray = np.array(image)  # cast as numpy array to check against alpha channel
            
            # Get cropping indices
            indices = np.where(imageArray[:, :, 3] > 0) # non-zero pixel bound indices
            x0, y0, x1, y1 = indices[1].min(), indices[0].min(), indices[1].max(), indices[0].max() # top-left -> bottom-right
            
            # Crop & Pad
            croppedImage = Image.fromarray(imageArray[y0:y1+1, x0:x1+1, :]) # crop
            paddedImage = AlphaCropper.AddPadding(croppedImage, self.transparencyPadding, self.transparencyPadding, self.transparencyPadding, self.transparencyPadding)

            # Save
            self.__SaveImage(paddedImage, nameImagePair.imageName, self.outputExtension)

    # Private Methods
    def __SetImagePaths(self):
        self.imagePathList.clear()
        directory = os.fsencode(self.inputPath)

        for file in os.listdir(directory):
            filename = os.fsdecode(file)
            for fileType in self.fileTypes:
                if filename.endswith(fileType):
                    self.imagePathList.append(os.path.join(self.inputPath, filename))
                    continue
                else:
                    continue

    def __SetImages(self):
        self.nameImagePairs.clear()
        for imagePath in self.imagePathList:
            imageName = os.path.basename(imagePath).split('.')[0]
            nameImagePair = NameImagePair(imageName, Image.open(imagePath))
            self.nameImagePairs.append(nameImagePair)
    
    def __SaveImage(self, image:Image, imageName:str, extension:str):
        savePath = os.path.join(self.outputPath, imageName + "." + extension)
        image.save(savePath)

# Main Execution
if __name__ == "__main__":
    alphaCropper = AlphaCropper()
    alphaCropper.LoadFromDefault()
    alphaCropper.CropImages()
    