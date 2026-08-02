This folder contains files for [Booth](https://anatawa12.booth.pm/items/4885109).

- thumbnail-1.svg, thumbnail-2.svg: thumbnail in svg format. you can get rendererd image with `make thumbnail` and as `.svg.png` extension files.
- Makefile: the makefile to build booth assets. `make all` to build all, `make thumbnail` to build thumbnails (requires rsvg with fontconfig backend), and `make assets` for building assets to be uploaded to booth.
- GenJyuuGothic: We use [GenJyuuGothic](http://jikasei.me/font/genjyuu/) for texts in thumbnail. This is the version we use, and will be used by `make thumbnail`.
- external-assets: The directory contains serveral exteraal assets. includes:
  - modular-avatar.png: logo image for Modular Avatar which was published under MIT License by bd_. Original image can be downloaded from https://github.com/bdunderscore/modular-avatar/blob/f7cec4d435035ce4d68471d18bf09a7ff4d266d8/Packages/nadena.dev.modular-avatar/Editor/Images/logo.png
