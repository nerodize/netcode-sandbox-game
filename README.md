# Repo Link
[Repository Link](https://github.com/nerodize/netcode-sandbox-game/)

# Warum nicht als Zip?
Die verwendeten Assets sind im aktuellen Stand noch zu groß (insgesamt an die 12GB) um hochgeladen zu werden.

# Wo ist was zu finden?
Der code findet sich unter ``Assets/scripts`` wieder und ist hier hauptsächlich im Unterordner ``Assets/scrits/Gun`` oder ``/Player`` zu finden.
Die erhaltenen Messwerte sind vor allem unter ``Assets/Measurements`` gespeichert und können eingesehen werden. 

Wenn das Spiel jedoch gestartet wird werden diese Daten in einen persistenten Ordner unter Unity gelegt: ``C:\Users\<WinUser>\AppData\LocalLow\<user_name>\Thesis`` und ``C:\Users\<WinUser>\AppData\LocalLow\<user_name>\Thesis\Logs``.

Desweiteren wird nicht jedes Skript verwendet, hierbei handelt es sich oft um eine Art _deprecated_ code, der noch aufgeräumt werden muss.


# Empfohlener Spielablauf

Um das _Spiel_ zu starten ist in erster Linie Unity 6 notwendig und sollte nach der Installation mit 2 Builds gestartet werden.
Am sinnvollsten ist es, wenn mit dem Build den Host startet. Hierfür wird nach Kompilierung des Builds der ``Host``-Button angeklickt.
Als effektiven Spieler wird der Playmodus des Editoren verwendet bzw. empfohlen. Wenn der Playmodus aktiv ist wird der ``Client``-Button angeklickt um als Client das Spiel zu betreteten.

## Änderung der Netzwerkparameter
In der Hierarchie sind ``Netzwerk-Simulator`` eine Komponente, die das Ändern der Latenz oder "Delay" zulässt. Dieser muss vor dem Ausführen des Spiels gespeichert werden um auf den Spielfluss zu wirken. 


## Steuerung
Die Steuerung ist sehr klassisch gehalten und verwendet für die Richtungstasten WASD (nach vorne, links, unten und rechts). Zum Springen die Leertaste und zum Schießen MB1 bzw. Linksklick und zum Nachladen ``R``.
