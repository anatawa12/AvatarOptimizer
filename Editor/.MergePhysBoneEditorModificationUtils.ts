/// <reference lib="es2020" />

type Config = Record<string, { 
    base: string, 
    values: [string, string][],
    noGenerateActiveProps?: boolean,
}>

const config: Config = {
    CurveConfigProp: {
        base: "OverridePropBase",
        values: [
            ['Value', 'value'],
            ['Curve', 'curve'],
        ],
    },
    PermissionConfigProp: {
        base: "OverridePropBase",
        values: [
            ['Value', 'value'],
            ['Filter', 'filter'],
        ],
        noGenerateActiveProps: true,
    },
    ValueConfigProp: {
        base: "OverridePropBase",
        values: [['Value', 'value']],
    },
    NoOverrideValueConfigProp: {
        base: "PropBase",
        values: [['Value', 'value']],
        noGenerateActiveProps: true,
    },
} satisfies Config;

console.log("// <generated />")
console.log("#nullable enable")
console.log("")
console.log("#if AAO_VRCSDK3_AVATARS")
console.log("")
console.log("using System.Collections.Generic;")
console.log("using UnityEditor;")
console.log("using JetBrains.Annotations;")
console.log("")
console.log("namespace Anatawa12.AvatarOptimizer")
console.log("{")
console.log("    partial class MergePhysBoneEditorModificationUtils")
console.log("    {")
for (let [type, info] of Object.entries(config)) {
    console.log(`        protected partial class ${type} : ${info.base}`)
    console.log(`        {`)
    for (let [value] of info.values) {
        console.log(`            public readonly SerializedProperty Override${value};`)
        console.log(`            public SerializedProperty? Source${value} { get; private set; }`)
        console.log(`            public readonly string PhysBone${value}Name;`)
    }
    console.log(``)
    console.log(`            public ${type}(`)
    console.log(`                SerializedProperty rootProperty`)
    for (let [value] of info.values) {
        console.log(`                , string physBone${value}Name`)
    }
    console.log(`                ) : base(rootProperty)`)
    console.log(`            {`)
    for (let [value, name] of info.values) {
        console.log(`                Override${value} = rootProperty.FindPropertyRelative("${name}");`)
        console.log(`                PhysBone${value}Name = physBone${value}Name;`)
    }
    console.log(`            }`)
    console.log(``)
    console.log(`            internal override void UpdateSource(SerializedObject sourcePb)`)
    console.log(`            {`)
    for (let [value] of info.values) {
        console.log(`                Source${value} = sourcePb.FindProperty(PhysBone${value}Name);`)
    }
    console.log(`            }`)
    for (let [value] of info.values) {
        console.log(`            public SerializedProperty Get${value}Property(bool @override) => @override ? Override${value} : Source${value}!;`)
    }
    console.log(`        }`)
}
console.log("    }")
console.log("}")
console.log("")
console.log("#endif")
