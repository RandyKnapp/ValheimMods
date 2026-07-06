# frozen_string_literal: true

require "openai"

#####
# This script can be used to generate translations (or missing chunks of translations) for each of the 26 languages valheim supports
# it is suggested to run this on Ruby 3.4.5+
# it requires the openai gem `gem install ruby-openai`, it also requires a valid OpenAI key to be placed in a .env file (in the same directroy)
# the .env file is gitignored and not present in the repo.
# this script can be ran like: `ruby create_update_language_files.rb`
#
#

file = File.open(".env")
config = {
  skip_existing: false
}
localizations_dir = "localizations"
file.readlines.map(&:chomp).each do |secret|
  secret_details = secret.split("=")
  config[secret_details[0]] = secret_details[1]
end

def sanitize_response(text, original_text)
  remove_newline = !original_text.include?("\n")
  sanitized_response = text.gsub("\"", "").gsub("”", "").gsub("“", "").gsub("#{original_text}", "").gsub("Translation:", "")
  # Format a bunch of the various ways chatgpt might respond
  sanitized_response = sanitized_response.gsub("\n", "") if remove_newline
  sanitized_response = sanitized_response[3..] if sanitized_response[0..2] == " - " || sanitized_response[0..2] == " = "
  sanitized_response = sanitized_response[4..] if sanitized_response[0..3] == " -> "
  sanitized_response = sanitized_response.chomp
  return sanitized_response
end

client = OpenAI::Client.new(access_token: config["OPENAI_KEY"], log_errors: true)

master_file = File.read("#{localizations_dir}/English.json")
master_json = JSON.parse(master_file)
target_keys = master_json.keys.count
languages = %w[
  Swedish
  French
  Italian
  German
  Spanish
  Russian
  Romanian
  Bulgarian
  Macedonian
  Finnish
  Danish
  Norwegian
  Icelandic
  Turkish
  Lithuanian
  Czech
  Hungarian
  Slovak
  Polish
  Dutch
  Portuguese_European
  Portuguese_Brazilian
  Chinese
  Chinese_Trad
  Japanese
  Korean
  Hindi
  Thai
  Croatian
  Georgian
  Greek
  Serbian
  Ukrainian
  Latvian
]
languages.each do |lang|
  puts "Starting translation work for #{lang}"
  current_language_translation = {}
  keys_needed = []
  need_translation_updates = false
  if File.file?("#{localizations_dir}/#{lang}.json") && config[:skip_existing] == false
    current_language_translation = JSON.parse(File.read("#{localizations_dir}/#{lang}.json").force_encoding("utf-8"))
    if target_keys > current_language_translation.keys.count
      need_translation_updates = true
      puts "Translation updated needed for #{lang} (#{target_keys} > #{current_language_translation.keys.count})"
    end
    if need_translation_updates
      master_json.each_key do |key|
        next if current_language_translation.key?(key)

        keys_needed << key
      end
    end
    next if need_translation_updates == false

    keys_needed.each do |key|
      result = false
      translation = ""
      loop do
        response = client.chat(
          parameters: {
            model: "gpt-3.5-turbo-1106", # Required.
            messages: [{ role: "user", content: "Please translate the quoted phrase to #{lang} and respond only the translation text: \"#{master_json[key]}\"" }],
            temperature: 0.7
          }
        )
        translation = sanitize_response(response["choices"][0]["message"]["content"], master_json[key])
        result = true unless translation.empty?

        break if result == true
      end
      puts "Translated: #{master_json[key]} -> | #{translation} |"
      current_language_translation[key] = translation
    end
    # This truncates the existing file and rewrites it with the new larger json structure
    File.open("#{localizations_dir}/#{lang}.json", "w") { |f| f.write(JSON.pretty_generate(current_language_translation)) }
    next
  end

  puts "Starting on language translation for: #{lang}"

  # Have to chunk the requests because max request size is 16385
  sanitized = ""
  if (target_keys * 100 > 1600)
    json_chunk = {}
    response_chunks = {}
    requestsize = 0
    counts = 0
    master_json.each do |k,v|
      counts += 1
      requestsize += k.length
      requestsize += v.length
      json_chunk[k] = v
      # Send the request and reset the page, or send the last batch
      if (requestsize >= 1600 || counts >= target_keys)
        puts "Built request with #{json_chunk.keys.count} keys"
        response = client.chat(
        parameters: {
          model: "gpt-3.5-turbo-1106", # Required.
          response_format: { type: "json_object" },
          messages: [{ role: "user", content: "Please convert the following JSON to #{lang}: #{json_chunk}" }],
          temperature: 0.7
        })
        json_chunk.clear
        requestsize = 0
        sanitized = response["choices"][0]["message"]["content"].force_encoding("utf-8")
        begin
          JSON.parse(sanitized).each do |k, v|
            response_chunks[k] = v
          end
        rescue StandardError # rubocop:disable Lint/SuppressedException
          Puts "Error deserializing batch, re-run to generate missing translations."
        end
      end
    end
    sanitized = JSON.generate(response_chunks)
  else 
      response = client.chat(
      parameters: {
        model: "gpt-4.1-mini", # Required.
        response_format: { type: "json_object" },
        messages: [{ role: "user", content: "Please convert the following JSON to #{lang}: #{master_file}" }],
        temperature: 0.7
      }
    )
    puts "Recieved response for #{lang}, writing to language file. choices #{response['choices'].length}"
    sanitized = response["choices"][0]["message"]["content"].force_encoding("utf-8")
  end



  file_completed = false
  begin
    language_json = JSON.parse(sanitized)
    File.write("#{localizations_dir}/#{lang}.json", JSON.pretty_generate(language_json))
    file_completed = true
  rescue StandardError # rubocop:disable Lint/SuppressedException
  end
  next if file_completed

  # do line-by-line translation to avoid JSON issues
  translated_hash = {}
  master_json.each do |key, value|
    response = client.chat(
      parameters: {
        model: "gpt-3.5-turbo-1106", # Required.
        messages: [{ role: "user", content: "Please translate the quoted phrase to #{lang} and respond only the translation text: \"#{value}\"" }],
        temperature: 0.7
      }
    )
    # Chatgpt likes to respond with the original string also, and likes to quote/not quote inconsistently
    # puts "Original response: #{response["choices"][0]["message"]["content"]}"

    sanitized_response = sanitize_response(response["choices"][0]["message"]["content"], value)
    puts "Line response: #{sanitized_response}"
    translated_hash[key.to_sym] = response["choices"][0]["message"]["content"].gsub("\"", "").force_encoding("utf-8")
  end
  File.write("#{localizations_dir}/#{lang}.json", JSON.pretty_generate(translated_hash))
end
