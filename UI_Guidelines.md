# I've searched through the SEB repository and found the relevant XAML files in the SafeExamBrowser.UserInterface.Desktop project. Here are the UI measurements extracted for your WinForms clone:

## 1. Taskbar

### Files:
- `Taskbar.xaml`
- `Clock.xaml`
- `AudioControl.xaml`
- `NetworkControl.xaml`

### Measurements:
- **Height:** 40px
- **Background Color:** `#FFF0F0F0` (uses `BackgroundBrush` from shared colors)
- **Clock Font:** Default WPF font (likely Segoe UI, 12px), with `FontWeight="Bold"` for the time and standard weight for the date. The clock container uses `Padding="10, 0, 10, 0"`.
- **Icon Spacing/Margins:** The icons (Audio, Network, etc.) do not use explicit margins between them. Instead, each icon button acts as a `40x40` block (`Width="40"`, matching the taskbar height) and applies an internal `Padding="5"`.

---

## 2. Action Center & Lock Screen

### Files:
- `ActionCenter.xaml`
- `LockScreen.xaml`

### Action Center Measurements:
- **Background Color:** `#EEF0F0F0` (uses `BackgroundTransparentBrush`)
- **Panel Dimensions:** Width: `400px`, Height: `1000px`
- **Control Margin:** The bottom control panel grid uses `Margin="10"`.

### Lock Screen Measurements:
- **Background Color:** Red (standard red brush)
- **Panel Dimensions:** `WindowState="Maximized"` (fills the screen)
- **Header Font:** `FontSize="50"`, `FontWeight="ExtraBold"`, `Foreground="White"` (Text: `"SEB LOCKED"`)
- **Body Font:** `FontSize="16"`, `FontWeight="DemiBold"`, `Foreground="White"`
- **Border Radius:** None specified (standard sharp corners). Password box uses `Margin="10, 5"`.

---

## 3. Browser Window Chrome

### File:
- `BrowserWindow.xaml`

### Measurements:
- **Title Bar:** There is no custom title bar defined in this file; it falls back to the native Windows OS chrome title bar. The main window background is `#FFF0F0F0`.

### Top Toolbar Dimensions:
- The toolbar itself has `Margin="5, 0"` and a bottom border of `BorderThickness="0, 0, 0, 1"` (LightGray).
- **Navigation Buttons (Back, Forward, Home, Reload, Menu):** `Height="30"`, `Margin="5"`.
- **URL Text Box:** `Height="25"`, `Margin="5, 0"`, `Padding="5, 0"`.

### Find Bar Dimensions (Bottom):
- Outer border has `Margin="5, 0"` and top border of `BorderThickness="0, 1, 0, 0"` (LightGray).
- Inner `StackPanel` container has `Height="25"`, `Margin="5, 0"`.

---

## 4. General Theme & Colors

### File:
- `Colors.xaml`

### Shared Color Palette:
- **ActionCenterDarkBrush:** `#AA808080`
- **BackgroundBrush:** `#FFF0F0F0`
- **BackgroundTransparentBrush:** `#EEF0F0F0`
- **BackgroundTransparentEmphasisBrush:** `#99D3D3D3`
- **PrimaryTextBrush:** Black (`#000000`)
- **SecondaryTextBrush:** DimGray (`#696969`)
