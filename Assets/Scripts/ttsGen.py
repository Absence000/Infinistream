import os, random, shutil, asyncio, fakeyou, requests, time
from spacesavers import *
from google.cloud import texttospeech

# with open(f"config.json") as file:
#     config = json.load(file)

config = openjson("config")

assetPath = config['ASSET_PATH']
scriptPath = f"{assetPath}/Scripts"


unknownvoiceList = {}

fyvoiceDict = config["FAKEYOU_VOICE_DICTIONARY"]

fyRandos = config["FAKEYOU_RANDO_VOICES"]
inputList = []
voiceDoneList = []

googleVoiceDict = config["GOOGLE_VOICE_DICTIONARY"]
googleRandos = config["GOOGLE_RANDO_VOICES"]

async def setupDialogue(sceneIndex):
    os.makedirs(f"{assetPath}/newMP3s/", exist_ok=True)
    os.makedirs(f"{config['AUDIO_REPO_PATH']}/{sceneIndex}", exist_ok=True)
    global inputList
    global voiceDoneList
    global sceneInd
    sceneInd = sceneIndex
    usingFakeYou = config["VOICE_SOURCE"] == "fakeyou"
    if usingFakeYou:
        randos = fyRandos
    else:
        randos = googleRandos
    tempRandos = randos.copy()
    subtitles = ""
    characterList = ""
    global unknownvoiceList
    script = opentxt("newscript", True)
    # Converts script into blocks of text per character
    lines = script.splitlines()
    index = 0
    for line in lines:
        if ":" in line and "FADE " not in line[:line.index(":")] and "SCENE" not in line[:line.index(":")].upper() and "CUT TO" not in line[:line.index(":")] and len(line[line.index(":"):]) > 5:
            #this is dialogue
            subtitles += line + "\n"
            character = line[:line.index(":")]
            if "/" in character:
                character = character.replace("/", " & ")

            for names in config["NICKNAMES"].items():
                for nickname in names[1]:
                    if character == nickname:
                        character = names[0]
            fixedName = character.upper().replace(" ", "_")
            if fixedName not in characterList.split(" "):
                characterList += f'{fixedName} '
                if len(tempRandos) > 0:
                    randIndex = random.randint(0, len(tempRandos)-1)
                    unknownvoiceList[character.lower()] = tempRandos[randIndex]
                    del tempRandos[randIndex]
                else:
                    unknownvoiceList[character.lower()] = random.choices(randos)[0]

            dialogue = line[line.index(":")+2:]
            # removes stuff between parentheses
            if "(" in dialogue and ")" in dialogue:
                dialogue = dialogue[:dialogue.index("(")] + dialogue[dialogue.index(")")+2:]
            if "laugh" in character:
                dialogue = "hahahahahaha"
            if len(character) < 25:
                #Input List
                inputList.append([index, dialogue, character])
            index += 1
        elif config["ENABLE_LAUGH_TRACK"] and "laugh" in line:
            subtitles += line + "\n"
            # this is a laugh track
            shutil.copyfile(f"{assetPath}/laugh.mp3",
                            f"{assetPath}/newMP3s/{index} - LAUGH.mp3")
            index += 1
        elif config["NARRATOR"]:
            if len(line) > 0:
                # if the narrator is enabled, it'll generate text for the other
                # lines
                if ":" in line:
                    line = line.replace(":", "")
                inputList.append([index, line, "narrator"])
                subtitles += line + "\n"
                index += 1
    # multithreading stuff
    if usingFakeYou:
        fy = fakeyou.AsyncFakeYou()
        await fy.login(config["FAKEYOU_USERNAME"], config["FAKEYOU_PASSWORD"])
        asyncList = []
        for input in inputList:
            asyncList.append(asyncio.ensure_future(createTTS(fy, input)))
            voiceDoneList.append("generating")
        print(f"Generating {len(voiceDoneList)} TTS files...")
        try:
            await asyncio.wait_for(asyncio.gather(*asyncList), timeout=180)
        except asyncio.TimeoutError:
            print("Timed out! Skipping remaining voices!")
        except Exception as e:
            print(e)
    else:
        for input in inputList:
            createGoogleTTS(input[0], input[1], input[2])
    savetxt("characterList", characterList)
    savetxt("audioInfo", "done")
    # Goes through each line of the subtitles and removes the timed out lines
    # not used right now
    # subtitleLines = subtitles.split("\n")
    # fixedSubtitles = ""
    # i = 0
    # for line in voiceDoneList:
    #     if line == "done!":
    #         fixedSubtitles += subtitleLines[i] + "\n"
    #     i += 1
    # savetxt("subtitles", fixedSubtitles)
    savetxt("subtitles", subtitles)
    print("done!")
    voiceDoneList = []
    unknownvoiceList = {}
    inputList = []


async def createTTS(fy, inputList):
    global unknownvoiceList
    #finds the correct voice using a voice dictionary
    if inputList[2].lower() in fyvoiceDict:
        name = fyvoiceDict[inputList[2].lower()]['name']
    else:
        #it uses the unknown voice dict if it doesn't find it (random voice)
        name = unknownvoiceList[inputList[2].lower()]
    #print(f"Generating voice #{inputList[0]}")
    voiceSucessfullyGenerated = False
    while not voiceSucessfullyGenerated:
        try:
            await sendRequest(fy, inputList, name)
            voiceSucessfullyGenerated = True
        except Exception as e:
            if str(e) != "('Too many requests, try again later or use a proxy.', 'Too many requests, try again later.')":
                print(e)
            await asyncio.sleep(5)


async def sendRequest(fy, inputList, name):
    global voiceDoneList
    result = await fy.say(inputList[1], name)
    r = requests.get(
        "https://storage.googleapis.com/vocodes-public" + result.json["maybe_public_bucket_wav_audio_path"])
    open(f"{assetPath}/newMP3s/" + f"{inputList[0]} - {inputList[2].upper()}.wav", 'wb').write(r.content)
    open(f"{config['AUDIO_REPO_PATH']}/{sceneInd}/" + f"{inputList[0]} - {inputList[2].upper()}.wav", 'wb').write(
        r.content)
    voiceDoneList[inputList[0]-1] = "done!"
    print(f"Finished generating voice #{inputList[0]}!")


failedTimes = 0

def createGoogleTTS(index, dialogue, character):
    try:
        mytext = dialogue

        client = texttospeech.TextToSpeechClient.from_service_account_info(openjson("googleCredentials"))

        input_text = texttospeech.SynthesisInput(text=mytext)

        # Gets info from characters
        if character.lower() in googleVoiceDict:
            voiceInfo = googleVoiceDict[character.lower()]
        else:
            global unknownvoiceList
            voiceInfo = unknownvoiceList[character.lower()]
        if voiceInfo['gender'] == "MALE":
            gender = texttospeech.SsmlVoiceGender.MALE
        else:
            gender = texttospeech.SsmlVoiceGender.FEMALE

        # Applies all the stuff
        voice = texttospeech.VoiceSelectionParams(
            language_code=voiceInfo["name"][0:5],
            name=voiceInfo["name"],
            ssml_gender=gender,
        )

        audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.MP3
        )

        response = client.synthesize_speech(
            request={"input": input_text, "voice": voice, "audio_config": audio_config}
        )

        # The response's audio_content is binary.
        with open(f"{assetPath}/newMP3s/" + f"{index} - {character.upper()}.mp3", "wb") as out:
            out.write(response.audio_content)

        with open(f"{config['AUDIO_REPO_PATH']}/{sceneInd}/" + f"{index} - {character.upper()}.mp3", "wb") as out:
            out.write(response.audio_content)

    except Exception as e:
        print(e)
        global failedTimes
        failedTimes += 1
        time.sleep(1)
        if failedTimes <= 10:
            print(f"Trying again: attempt {failedTimes}/10")
            createGoogleTTS(index, dialogue, character)
        else:
            #skips the line of dialogue
            print("Skipping dialogue!")
            failedTimes = 0

# sceneInd = 1
# createGoogleTTS(1, "Bitch.", "JESSE")