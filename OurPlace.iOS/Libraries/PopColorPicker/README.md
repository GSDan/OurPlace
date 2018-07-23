##PopColorPicker

A color picker component for Xamarin.iOS.

Support both iPhone and iPad.

![AdvancedColorPicker](https://github.com/has606/PopColorPicker/blob/master/Images/screenshot_iphone.png)

![AdvancedColorPicker](https://github.com/has606/PopColorPicker/blob/master/Images/screenshot_ipad.png)

### Features

* Custom color
* Save favorite colors
* Press-and-hold to delete a favorite color
* Random colors
* Color palette

### Usage

```csharp
colorPickerViewController = new ColorPickerViewController();

// assign event for cancelling
colorPickerViewController.CancelButton.Clicked += (object sender, EventArgs e) => {
	if (UserInterfaceIdiomIsPhone) { // for iPhone
		DismissViewController(true, null);
	} else { // for iPad
		_popoverController.Dismiss(true);
	}
};

// assign event for done selecting
colorPickerViewController.DoneButton.Clicked += (object sender, EventArgs e) => {
	if (UserInterfaceIdiomIsPhone) { // for iPhone
		DismissViewController(true, null);
	} else { // for iPad
		_popoverController.Dismiss(true);
	}

	var selectedColor = ccolorPickerViewController.SelectedColor;
};
```
Please check out the sample project about how to use it.

### License

PopColorPicker is licensed under the terms of the MIT license.


