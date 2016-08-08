# Pokémon Go Client API

Interface to Pokémon Go Client including pretty much every call

**Read previous issues before opening a new one! Maybe your issue is already answered. Questions will be removed.

Donate some bitcoin beers via: 1KRBmNN8y9hwaXGR7rCoJWmzJHfBp2ybRb

----------
### Usage Example

```
var client = new Client(new Settings()); //Define your own ISettings implementation
await _client.Login.DoGoogleLogin();
var inventory = await _client.Inventory.GetInventory();
var profile = await _client.Player.GetOwnProfile();
var playerStats = await _inventory.GetPlayerStats();
var settings = await _client.Download.GetSettings();
var mapObjects = await _client.Map.GetMapObjects();
var updateLocation = await _client.Player.UpdatePlayerLocation();
var encounter = await _client.Encounter.EncounterPokemon(encId, spawnId);
var catchPokemon = await _client.Encounter.CatchPokemon(pokemon.EncounterId, pokemon.SpawnPointId, pokeball)
var evolvePokemon = await _client.Inventory.EvolvePokemon(pokemonId);
var transfer = await _client.Inventory.TransferPokemon(pokemonId);
var recycle = await _client.Inventory.RecycleItem(item.ItemId, item.Count);
var useBerry = await _client.Encounter.UseCaptureItem(encounterId, ItemId.ItemRazzBerry, spawnPointId);
var fortInfo = await _client.Fort.GetFort(pokeStopId, pokeStopLatitude, pokeStopLongitude);
var fortSearch = await _client.Fort.SearchFort(pokeStopId, pokeStopLatitude, pokeStopLongitude);

and a lot more :)

You can visit Pokestops, encounter Pokemon (normal/lure/incense), catch Pokemon, drop items, use items and everything else :)
```

----------

### What is Pokémon Go?
According to [the company](http://www.pokemon.com/us/pokemon-video-games/pokemon-go/):

> “Travel between the real world and the virtual world of Pokémon with Pokémon GO for iPhone and Android devices. With Pokémon GO, you’ll discover Pokémon in a whole new world—your own! Pokémon GO is built on Niantic’s Real World Gaming Platform and will use real locations to encourage players to search far and wide in the real world to discover Pokémon. Pokémon GO allows you to find and catch more than a hundred species of Pokémon as you explore your surroundings.”

# License

This Project is licensed as GNU (GNU GENERAL PUBLIC LICENSE v3) 

## Legal

This code is in no way affiliated with, authorized, maintained, sponsored or endorsed by Niantic, The Pokémon Company, Nintendo or any of its affiliates or subsidiaries. This is an independent and unofficial API for educational use ONLY. Use at your own risk.

## Credits

Credits to AeonLucid, johnduhart and for making public proto available. Saved a lot of work!
