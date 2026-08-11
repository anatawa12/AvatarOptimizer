This folder contains files for [Booth](https://anatawa12.booth.pm/items/4885109).

- thumbnail-1.svg, thumbnail-2.svg: thumbnails in SVG format. You can render them with `make thumbnail` (outputs `*.svg.png`).
- Makefile: the makefile to build Booth assets. `make all` builds everything; `make thumbnail` builds thumbnails (requires rsvg with fontconfig backend; thumbnail-1 also needs VRChat SDK base at `../com.vrchat.base` as normal); and `make assets` builds assets to be uploaded to Booth.
- GenJyuuGothic: We use [GenJyuuGothic](http://jikasei.me/font/genjyuu/) for text in thumbnails. This is the version we use, and will be used by `make thumbnail`.
- external-assets: The directory contains several external assets, including:
  - modular-avatar: logo image for Modular Avatar which is at <https://modular-avatar.nadena.dev/docs/distributing-prefabs/logo-usage>.
