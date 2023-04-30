# Say goodbye to charcoal shortages, hello to explosives!
With BetterCharcoal, your players will no longer have to rely on mass campfires to secure enough of this vital resource. By simple gathering some wood and igniting the furnaces, players will have a steady flow of charcoal pour out - the key ingredient for every successful raid!

![](https://i.imgur.com/V0kGAT1.png)
  
-------
  
## Continuous charcoal production
Any furnaces that were operating before a server restart or during a plugin reload will automatically resume yielding charcoal, eliminating any interruptions in production.
  
----- 


## Electric furnaces
Gone are the days of relying solely on wood to fuel your furnaces. With BetterCharcoal, your electric furnaces will not only eliminate the need for wood, but also double the efficiency with charcoal production!

![](https://i.imgur.com/zspNsiX.png)

Don't let the science fool you, electricity is all you need to get the job done.

-----

## Permissions
* `bettercharcoal.use` - Players with this permission will have their ovens produce charcoal differently.

-------

## Configuration
```json
{
  "Version": "2.2.0",
  "Enable Charcoal Production": true,
  "Charcoal Yield Chance": 75,
  "Lowest Charcoal Yield": 1,
  "Highest Charcoal Yield": 1,
  "Charcoal Production Rate": 1,
  "Fuel Consumption Rate": 1,
  "Enable Electric Furnace Charcoal Production": false,
  "Electric Furnace Charcoal Yield Interval": 2.0
}
```


### Enable Charcoal Production
This option determines whether or not charcoal creation is enabled. If set to false, ovens will not produce charcoal at all. Defaults to true.

### Charcoal Yield Chance
This setting determines the chance that charcoal will be produced each time the oven burns. Defaults to 75 percent.

### Charcoal Yields
A random amount of charcoal is produced each time the oven burns wood. To ensure a fixed amount of charcoal is produced, set both the minimum and maximum yields to the same number. Both minimum and maximum default to 1.

### Charcoal Production Rate
This setting determines the rate of charcoal production. For example, if 5 charcoal is produced per burn and the rate is 2, the final output will be 10. Defaults to 1 charcoal.

### Fuel Consumption Rate
This value determines how much wood is consumed during each burn. Defaults to 1 fuel.

### Charcoal Yield Interval
This setting specifies the time it takes for an electric furnace to produce a set amount of charcoal. This option only applies to electric furnaces, as the charcoal production interval for regular ovens is based on the consumption of wood.

-------

## Keep the mod alive

Creating plugins is my passion, and I love nothing more than exploring new ideas and bringing them to the community. But it takes hours of work every day to maintain and improve these plugins that you have come to love and rely on. 

With your support on [Patreon](https://www.patreon.com/VisEntities), you're  giving me the freedom to devote more time and energy into what I love, which in turn allows me to continue providing new and exciting content to the community.

![](https://i.imgur.com/6x7kn9f.png)

A portion of the contributions will also be donated to the uMod team as a token of appreciation for their dedication to coding quality, inspirational ideas, and time spent for the community.

-------

## Credits
* Originally created by **Jake_Rich**, up to version 1.0.0
* Completely rewritten from scratch and maintained to present by **Dana**.