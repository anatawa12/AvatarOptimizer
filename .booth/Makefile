
all: thumbnail assets

export PANGOCAIRO_BACKEND:=fontconfig
export FONTCONFIG_FILE:=$(CURDIR)/fonts.conf

%.svg.png: %.svg $(FONTCONFIG_FILE) GenJyuuGothic/GenJyuuGothic-Bold.ttf GenJyuuGothic/GenJyuuGothic-Medium.ttf
	rsvg-convert $< > $@

thumbnail-1.svg.png: tmp/sdk-poor.png tmp/sdk-very-poor.png tmp/sdk-good.png tmp/sdk-medium.png external-assets/modular-avatar.png
thumbnail-2.svg.png: thumbnail-2-before.png thumbnail-2-after.png

SDK_PERF_ICONS:=../../com.vrchat.base/Runtime/VRCSDK/Dependencies/VRChat/Resources/PerformanceIcons
tmp/sdk-good.png: $(SDK_PERF_ICONS)/Perf_Good_32.png
tmp/sdk-medium.png: $(SDK_PERF_ICONS)/Perf_Medium_32.png
tmp/sdk-poor.png: $(SDK_PERF_ICONS)/Perf_Poor_32.png
tmp/sdk-very-poor.png: $(SDK_PERF_ICONS)/Perf_Horrible_32.png

tmp/%:
	mkdir -p $(dir $@) && cp $< $@

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
