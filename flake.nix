{
  description = "Development shell";

  inputs.nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";
  inputs.dalamud = {
    url = "github:Blooym/dalamud-nix";
    inputs.nixpkgs.follows = "nixpkgs";
  };

  outputs =
    { nixpkgs, dalamud, ... }:
    let
      forAllSystems =
        function:
        nixpkgs.lib.genAttrs nixpkgs.lib.systems.flakeExposed (
          system: (function system nixpkgs.legacyPackages.${system})
        );
    in
    {
      devShells = forAllSystems (
        system: pkgs:
        let
          dalamudPkg = dalamud.packages.${system}.stg;
        in
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
              dalamudPkg.dotnetSdk
              rustc
              cargo
              rustfmt
              clippy
              bashInteractive
            ];
            env = {
              RUST_SRC_PATH = pkgs.rustPlatform.rustLibSrc;
              DALAMUD_HOME = dalamudPkg;
            };
          };
        }
      );
    };
}
