# Image Reorder

A Windows desktop app for arranging images in a folder and renaming them to match the chosen order.

## Features

- Load a folder through **File → Open Folder** or by dropping a folder into the window.
- View supported images as thumbnails, initially sorted by filename.
- Drag thumbnails to arrange the sequence.
- Preview each image's planned name before applying changes.
- Rename images to `IMG_0001`, `IMG_0002`, and so on, preserving each file's extension.
- Check for filename collisions before renaming. Applying names changes files on disk and requires confirmation.

Supported extensions: JPG, JPEG, JFIF, PNG, GIF, BMP, TIF, and TIFF.

## Requirements

- Windows
- .NET 10 SDK

## Run

From the project directory:

```powershell
dotnet run --project ImageReorder.csproj
```

## Test

```powershell
dotnet test ImageReorder.Tests/ImageReorder.Tests.csproj
```

## Logs

Use **Help → Open Log File** or **Help → Open Log Folder** in the app to inspect diagnostic logs.
