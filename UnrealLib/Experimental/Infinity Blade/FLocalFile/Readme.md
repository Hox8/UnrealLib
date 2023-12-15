This is where I'm dumping information I find regarding IB3 saves as I discover it

## LocalFileCache
Manifest containing a list of all related filenames + their hashes.

## _CTN and _CTRB files
`_CTN:` _ClientTimeNewest_
`_CTRB:` _ClientTimeRolledBack_

Both exist as part of the `SwordSecureTime` class. Exact purpose not
yet known.

## _SwordSaveSlotX and _SwordSave files
Standard save files. Encrypted _and_ compressed.

# native WriteLocalFiles()
Responsible for writing game saves (data, slot) + metadata files

# native WriteLocalFileHeader()
Responsible for writing LocalFileHeaderCache file. Called within WriteLocalFiles()

# native WriteUserCredentials()
Was used for MyMob-related data. Unused since the servers were shutdown.
