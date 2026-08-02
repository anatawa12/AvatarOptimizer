
all: thumbnail assets

export PANGOCAIRO_BACKEND:=fontconfig
export FONTCONFIG_FILE:=$(CURDIR)/fonts.conf

%.svg.png: %.svg $(FONTCONFIG_FILE) GenJyuuGothic/GenJyuuGothic-Bold.ttf GenJyuuGothic/GenJyuuGothic-Medium.ttf
	rsvg-convert $< > $@

thumbnail-1.svg.png: external-assets/poor.png external-assets/very-poor.png external-assets/good.png external-assets/medium.png external-assets/modular-avatar.png
thumbnail-2.svg.png: thumbnail-2-before.png thumbnail-2-after.png

.PHONY: thumbnail
thumbnail: thumbnail-1.svg.png thumbnail-2.svg.png

.PHONY: AvatarOptimizer-1.x.x-installer.unitypackage
AvatarOptimizer-1.x.x-installer.unitypackage:
	curl -sL "https://api.anatawa12.com/create-vpai/?repo=https://vpm.anatawa12.com/vpm.json&package=com.anatawa12.avatar-optimizer&version=1.x.x" > $@

LICENSE.txt: ../LICENSE
	cp $< $@

.PHONY: assets
assets: AvatarOptimizer-1.x.x.zip

AvatarOptimizer-1.x.x.zip: AvatarOptimizer-1.x.x-installer.unitypackage README.ja.txt README.en.txt LICENSE.txt add-repo.url
	rm -f $@ && zip $@ $^
