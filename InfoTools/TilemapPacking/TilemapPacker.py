from PIL import Image
from scipy import stats
import math
import os

# Classes / Methods
class TilemapPacker:
    #Initialization
    def __init__(self) -> None:
        # Tunables
        self.inputPath = "Input"
        self.outputPath = "Output"
        self.fileTypes = list([".png", ".PNG"])
        self.extrusionPadding = 1
        self.transparencyPadding = 1

        # State
        self.imagePathList:list[str] = list()
        self.images:list[Image.Image] = list()
        self.tileHeight = 0
        self.tileWidth = 0
        self.compositeImage:Image.Image = Image.Image()
    
    # Static Methods
    @staticmethod
    def FilterImagesByResolution(images : list[Image.Image], tileWidth : int, tileHeight : int) -> list[Image.Image]:
        filteredImages : list[Image.Image] = list()
        for image in images:
            if (image.width != tileWidth or image.height != tileHeight):
                print(f"Warning {image} not included due to resolution mismatch")
                continue
            filteredImages.append(image)
        return filteredImages

    @staticmethod
    def CropImage(image : Image.Image) -> Image.Image:
        boundingBox = image.getbbox()
        return image.crop(boundingBox)

    @staticmethod
    def ReckonImageResolution(images : list[Image.Image]) -> tuple[int, int]:
        widths:list[int] = list()
        heights:list[int] = list()
        for image in images:
            widths.append(image.width)
            heights.append(image.height)

        return tuple[int, int]([stats.mode(widths).mode[0], stats.mode(heights).mode[0]])

    @staticmethod
    def AddPadding(image : Image.Image, left : int, right : int, top : int, bottom : int) -> Image.Image:
        paddedImage = Image.new(image.mode, (image.width + left + right, image.height + top + bottom), (0, 0, 0, 0))
        paddedImage.paste(image, (left, top))
        return paddedImage

    @staticmethod
    def ExtrudeEdges(image : Image.Image, padding : int):
        if (padding <= 0):
            return

        # Grab edges
        leftEdge = image.crop((0, 0, 1, image.height))
        rightEdge = image.crop((image.width - 1, 0, image.width, image.height))
        topEdge = image.crop((0, 0, image.width, 1))
        bottomEdge = image.crop((0, image.height - 1, image.width, image.height))
        
        # Extrude left/right edges up/down to account for corners
        topLeft = image.crop((0, 0, 1, 1))
        bottomLeft = image.crop((0, image.height - 1, 1, image.height))
        leftEdge = TilemapPacker.AddPadding(leftEdge, 0, 0, padding, padding)

        topRight = image.crop((image.width - 1, 0, image.width, 1))
        bottomRight = image.crop((image.width - 1, image.height - 1, image.width, image.height))
        rightEdge = TilemapPacker.AddPadding(rightEdge, 0, 0, padding, padding)

        for i in range(0, padding):
            leftEdge.paste(topLeft, (0, i))
            leftEdge.paste(bottomLeft, (0, image.height + padding + i))
            rightEdge.paste(topRight, (0, i))
            rightEdge.paste(bottomRight, (0, image.height + padding + i))

        # Add repeated edges onto image
        extrudedImage = TilemapPacker.AddPadding(image, padding, padding, padding, padding)

        for i in range(0, padding):
            extrudedImage.paste(leftEdge, (i, 0))
            extrudedImage.paste(rightEdge, (image.width + padding + i, 0))
            extrudedImage.paste(topEdge, (padding, i))
            extrudedImage.paste(bottomEdge, (padding, image.height + padding + i))

        # return
        return extrudedImage

    @staticmethod
    def GetRowColumnCountForComposite(imageCount : int) -> tuple[int, int]:
        imageCountRoot = math.sqrt(imageCount)
        columns = math.ceil(imageCountRoot)
        rows = math.floor(imageCountRoot)
        while rows * columns < imageCountRoot:
            rows = rows + 1
        return tuple([rows, columns])

    # Public Methods
    def LoadFromDefault(self):
        self.__SetImagePaths()
        self.__SetImages()
        [self.tileWidth, self.tileHeight] = TilemapPacker.ReckonImageResolution(self.images)
        self.__FilterImages()

    def PackImagesToComposite(self):
        if (len(self.images) == 0):
            return

        [rows, columns] = TilemapPacker.GetRowColumnCountForComposite(len(self.images))
        finalTileWidth = self.tileWidth + self.extrusionPadding*2 + self.transparencyPadding*2
        finalTileHeight = self.tileHeight + self.extrusionPadding*2 + self.transparencyPadding*2
        self.compositeImage = Image.new(self.images[0].mode, (finalTileWidth * columns, finalTileHeight * rows), (0, 0, 0, 0))

        columnCount = 0
        rowCount = 0
        for image in self.images:
            extrudedImage = TilemapPacker.ExtrudeEdges(image, self.extrusionPadding)
            paddedImage = TilemapPacker.AddPadding(extrudedImage, self.transparencyPadding, self.transparencyPadding, self.transparencyPadding, self.transparencyPadding)

            self.compositeImage.paste(paddedImage, (columnCount * finalTileWidth, rowCount * finalTileHeight))
            if (columnCount == columns):
                columnCount = 0
                rowCount = rowCount + 1
            else:
                columnCount = columnCount + 1
    
    def SaveComposite(self, compositeName:str, extension:str):
        savePath = os.path.join(self.outputPath, compositeName + "." + extension)
        self.compositeImage.save(savePath)

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
        self.images.clear()
        for imagePath in self.imagePathList:
            self.images.append(Image.open(imagePath))
    
    def __FilterImages(self):
        self.images = TilemapPacker.FilterImagesByResolution(self.images, self.tileWidth, self.tileHeight)

# Main Execution
tilemapPacker = TilemapPacker()
tilemapPacker.LoadFromDefault()
