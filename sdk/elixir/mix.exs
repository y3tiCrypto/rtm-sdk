defmodule Raptoreum.MixProject do
  use Mix.Project

  def project do
    [
      app: :rtm_sdk,
      version: "1.0.0",
      elixir: "~> 1.12",
      start_permanent: Mix.env() == :prod,
      deps: deps()
    ]
  end

  def application do
    [
      extra_applications: [:logger, :inets, :ssl]
    ]
  end

  defp deps do
    [
      {:jason, "~> 1.3"}
    ]
  end
end
