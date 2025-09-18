from enum import Enum, auto
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

@dataclass
class PaddingRequirements:
    left : int
    right : int
    top : int
    bottom : int

class ProgramSelector(Enum):
    TestSingleInput = auto()
    RunRecursively = auto()

# Classes / Methods
class AlphaPreMultiply:
    @staticmethod
    def PreMultiplyAlpha(image : Image.Image) -> Image.Image:
        imageArray = np.array(image).astype(np.double)

        alphaLinear = np.power((imageArray[:, :, 3] / 255.0), 2.2)
        redLinearPreMultiplied = np.power((imageArray[:, :, 0] / 255.0), 2.2) * alphaLinear
        greenLinearPreMultiplied = np.power((imageArray[:, :, 1] / 255.0), 2.2) * alphaLinear
        blueLinearPreMultiplied = np.power((imageArray[:, :, 2] / 255.0), 2.2) * alphaLinear

        imageArray[:, :, 0] = np.power(redLinearPreMultiplied, (1/2.2)) * 255.0
        imageArray[:, :, 1] = np.power(greenLinearPreMultiplied, (1/2.2)) * 255.0
        imageArray[:, :, 2] = np.power(blueLinearPreMultiplied, (1/2.2)) * 255.0
        imageArray[:, :, 3] = np.power(alphaLinear, (1/2.2)) * 255.0
        imageArray = np.clip(imageArray, 0, 255).astype(np.uint8)
        return Image.fromarray(imageArray)

    @staticmethod
    def CheckForContentOnEdges(image : Image.Image) -> PaddingRequirements:
        imageArray = np.array(image)
        alphaChannel = imageArray[:, :, 3]
        paddingRequirements = PaddingRequirements(0, 0, 0, 0)
        paddingRequirements.top = 1 if np.any(alphaChannel[0, :] > 0) else 0
        paddingRequirements.bottom = 1 if np.any(alphaChannel[-1, :] > 0) else 0
        paddingRequirements.left = 1 if np.any(alphaChannel[:, 0] > 0) else 0
        paddingRequirements.right = 1 if np.any(alphaChannel[:, -1] > 0) else 0
        return paddingRequirements

    @staticmethod
    def AddPadding(image : Image.Image, paddingRequirements : PaddingRequirements) -> Image.Image:
        if (paddingRequirements.left == 0 and paddingRequirements.right == 0 and paddingRequirements.top == 0 and paddingRequirements.bottom == 0): return image

        paddedImage = Image.new(image.mode, (image.width + paddingRequirements.left + paddingRequirements.right, image.height + paddingRequirements.top + paddingRequirements.bottom), (0, 0, 0, 0))
        paddedImage.paste(image, (paddingRequirements.left, paddingRequirements.top))
        return paddedImage

    @staticmethod
    def ShowAlphaAsBlackAndWhite(image : Image.Image) -> None:
        imageArray = np.array(image)
        alphaChannel = imageArray[:, :, 3]
        blackAndWhiteArray = np.stack((alphaChannel,)*3, axis=-1) # replicate alpha channel to RGB
        blackAndWhiteImage = Image.fromarray(blackAndWhiteArray, mode="RGB")
        blackAndWhiteImage.show()

    @staticmethod
    def ShowRGBChannels(image : Image.Image) -> None:
        imageArray = np.array(image)
        redChannel = imageArray[:, :, 0]
        greenChannel = imageArray[:, :, 1]
        blueChannel = imageArray[:, :, 2]

        redImageArray = np.stack((redChannel, np.zeros_like(redChannel), np.zeros_like(redChannel)), axis=-1)
        greenImageArray = np.stack((np.zeros_like(greenChannel), greenChannel, np.zeros_like(greenChannel)), axis=-1)
        blueImageArray = np.stack((np.zeros_like(blueChannel), np.zeros_like(blueChannel), blueChannel), axis=-1)

        redImage = Image.fromarray(redImageArray, mode="RGB")
        greenImage = Image.fromarray(greenImageArray, mode="RGB")
        blueImage = Image.fromarray(blueImageArray, mode="RGB")
        redImage.show()
        greenImage.show()
        blueImage.show()

    @staticmethod
    def PreMultiplySingle(imagePath : str, fileTypes : list[str], writeImageToFile : bool = False) -> None:
        os.path.isfile(imagePath)
        if os.path.isfile(imagePath) and any(imagePath.endswith(fileType) for fileType in fileTypes):
            print(f'On image: {imagePath}')
            image = Image.open(imagePath).convert("RGBA")
            
            paddingRequirements = AlphaPreMultiply.CheckForContentOnEdges(image)
            image = AlphaPreMultiply.AddPadding(image, paddingRequirements)

            AlphaPreMultiply.ShowRGBChannels(image)
            AlphaPreMultiply.ShowAlphaAsBlackAndWhite(image)

            preMultipliedImage = AlphaPreMultiply.PreMultiplyAlpha(image)
            AlphaPreMultiply.ShowRGBChannels(preMultipliedImage)
            
            if writeImageToFile:
                preMultipliedImage.save(imagePath)
        return

    def PreMultiplyRecursively(directory : str, fileTypes : list[str]) -> None:
        for entry in os.scandir(directory):
            if entry.is_dir():
                AlphaPreMultiply.PreMultiplyRecursively(entry.path, fileTypes)
            elif entry.is_file() and any(entry.name.endswith(fileType) for fileType in fileTypes):
                print(f'On image: {entry.path}')
                image = Image.open(entry.path).convert("RGBA")

                paddingRequirements = AlphaPreMultiply.CheckForContentOnEdges(image)
                image = AlphaPreMultiply.AddPadding(image, paddingRequirements)
                
                preMultipliedImage = AlphaPreMultiply.PreMultiplyAlpha(image)
                preMultipliedImage.save(entry.path)
        return

# Main Execution
if __name__ == "__main__":
    # Tunables
    programSelector = ProgramSelector.RunRecursively
    singleImageInputPath = './Input/GB_Narrow_Blue.png'
    resursiveInputPath = './InputDirectory'
    fileTypes = list([".png", ".PNG", ".Png"])

    match programSelector:
        case ProgramSelector.TestSingleInput:
            AlphaPreMultiply.PreMultiplySingle(singleImageInputPath, fileTypes, True)
        case ProgramSelector.RunRecursively:
            AlphaPreMultiply.PreMultiplyRecursively(resursiveInputPath, fileTypes)
