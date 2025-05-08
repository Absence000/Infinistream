import fakeyou, requests, os, asyncio

scriptPath = os.getcwd()


assetPath = os.path.abspath(os.path.join(scriptPath, os.pardir))


async def testFakeYou():
    fy = fakeyou.AsyncFakeYou()
    await fy.login("USERNAME", "PASSWORD")
    print("loading...")
    result = await fy.say("This video is sponsored by Raid: Shadow Legends!", "TM:9w9c91mzp34g")
    r = requests.get("https://storage.googleapis.com/vocodes-public"+result.json["maybe_public_bucket_wav_audio_path"])
    open(f"{assetPath}\\newMP3s\\" + f"test.wav", 'wb').write(r.content)
    print(f"Finished!")

asyncio.run(testFakeYou())

