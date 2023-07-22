push-files:
	-rm -rf ./Bin64/Plugins/Local/*
	-cp ./SpaceEngineersVR/bin/Debug/net48/win-x64/* ./Bin64/Plugins/Local/
	-mkdir ./Bin64/Plugins/Local/SEVRAssets/
	-cp -R ./SpaceEngineersVR/Assets/* ./Bin64/Plugins/Local/SEVRAssets/
  