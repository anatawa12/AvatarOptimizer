#!/usr/bin/env node

import { writeFileSync } from 'node:fs';

const baseUrl = process.argv[2];
const newVersion = process.argv[3];
const requiredUnity = "2022.3";

const supports2019 = requiredUnity.startsWith("2019");

if (supports2019) {
    writeFileSync("static/latest.txt", newVersion);
} else {
    const existingLatestTxt = await fetch(`${baseUrl}/latest.txt`);
    if (!existingLatestTxt.ok) throw new Error(`Failed to fetch latest.txt: ${existingLatestTxt.statusText}`);
    const existingLatest = await existingLatestTxt.text();
    writeFileSync("static/latest.txt", existingLatest);
}

const existingLatest2Txt = await fetch(`${baseUrl}/latest2.txt`);
if (!existingLatest2Txt.ok) throw new Error(`Failed to fetch latest2.txt: ${existingLatest2Txt.statusText}`);
const existingLatest2 = await existingLatest2Txt.text();

const parsed = existingLatest2.trimEnd().split("\n").map(x => x.split(':'));

const sameUnityIndex = parsed.findIndex(x => x[1] === requiredUnity);
if (sameUnityIndex !== -1) {
    parsed[sameUnityIndex][0] = newVersion;
} else {
    parsed.push([newVersion, requiredUnity]);
}

const newLatest2 = parsed.map(x => x.join(':') + '\n').join('');

writeFileSync("static/latest2.txt", newLatest2);
